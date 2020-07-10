using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class DecalSubTarget : HDSubTarget, ILegacyTarget, IRequiresData<DecalData>
    {
        public DecalSubTarget() => displayName = "Decal";

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template";
        protected override string[] templateMaterialDirectories =>  new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates",
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/"
        };
        protected override string subTargetAssetGuid => "3ec927dfcb5d60e4883b2c224857b6c2";
        protected override string customInspector => "Rendering.HighDefinition.DecalGUI";
        protected override string renderType => HDRenderTypeTags.Opaque.ToString();
        protected override string renderQueue => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, decalData.drawOrder, false, false));
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Decal;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Decal Subshader", "");
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl";

        // Material Data
        DecalData m_DecalData;

        // Interface Properties
        DecalData IRequiresData<DecalData>.data
        {
            get => m_DecalData;
            set => m_DecalData = value;
        }

        // Public properties
        public DecalData decalData
        {
            get => m_DecalData;
            set => m_DecalData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return PostProcessSubShader(SubShaders.Decal);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            pass.keywords.Add(CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true));

            // Emissive pass only have the emission keyword
            if (pass.lightMode == DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive])
            {
                if (decalData.affectsEmission)
                    pass.keywords.Add(DecalDefines.Emission);
            }
            else
            {
                if (decalData.affectsAlbedo)
                    pass.keywords.Add(DecalDefines.Albedo);
                if (decalData.affectsNormal)
                    pass.keywords.Add(DecalDefines.Normal);
                if (decalData.affectsMaskmap)
                    pass.keywords.Add(DecalDefines.Maskmap);
            }
        }

        public static FieldDescriptor AffectsAlbedo =           new FieldDescriptor(kMaterial, "AffectsAlbedo", "");
        public static FieldDescriptor AffectsNormal =           new FieldDescriptor(kMaterial, "AffectsNormal", "");
        public static FieldDescriptor AffectsEmission =         new FieldDescriptor(kMaterial, "AffectsEmission", "");
        public static FieldDescriptor AffectsMetal =            new FieldDescriptor(kMaterial, "AffectsMetal", "");
        public static FieldDescriptor AffectsAO =               new FieldDescriptor(kMaterial, "AffectsAO", "");
        public static FieldDescriptor AffectsSmoothness =       new FieldDescriptor(kMaterial, "AffectsSmoothness", "");
        public static FieldDescriptor AffectsMaskMap =          new FieldDescriptor(kMaterial, "AffectsMaskMap", "");
        public static FieldDescriptor DecalDefault =            new FieldDescriptor(kMaterial, "DecalDefault", "");

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
            context.AddField(AffectsAlbedo,        decalData.affectsAlbedo);
            context.AddField(AffectsNormal,        decalData.affectsNormal);
            context.AddField(AffectsEmission,      decalData.affectsEmission);
            context.AddField(AffectsMetal,         decalData.affectsMaskmap);
            context.AddField(AffectsAO,            decalData.affectsMaskmap);
            context.AddField(AffectsSmoothness,    decalData.affectsMaskmap);
            context.AddField(AffectsMaskMap,       decalData.affectsMaskmap);
            context.AddField(DecalDefault,         decalData.affectsAlbedo || decalData.affectsNormal || decalData.affectsMetal ||
                                                                    decalData.affectsAO || decalData.affectsSmoothness );
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            
            // Decal
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(HDBlockFields.SurfaceDescription.NormalAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.MAOSAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new DecalPropertyBlock(decalData));
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            Vector1ShaderProperty drawOrder = new Vector1ShaderProperty();
            drawOrder.overrideReferenceName = "_DrawOrder";
            drawOrder.displayName = "Draw Order";
            drawOrder.floatType = FloatType.Integer;
            drawOrder.hidden = true;
            drawOrder.value = 0;
            collector.AddShaderProperty(drawOrder);

            Vector1ShaderProperty decalMeshDepthBias = new Vector1ShaderProperty();
            decalMeshDepthBias.overrideReferenceName = "_DecalMeshDepthBias";
            decalMeshDepthBias.displayName = "DecalMesh DepthBias";
            decalMeshDepthBias.hidden = true;
            decalMeshDepthBias.floatType = FloatType.Default;
            decalMeshDepthBias.value = 0;
            collector.AddShaderProperty(decalMeshDepthBias);

            if (decalData.affectsAlbedo)
                AddAffectsProperty(HDMaterialProperties.kAffectsAlbedo);
            if (decalData.affectsNormal)
                AddAffectsProperty(HDMaterialProperties.kAffectsNormal);
            if (decalData.affectsAO)
                AddAffectsProperty(HDMaterialProperties.kAffectsAO);
            if (decalData.affectsMetal)
                AddAffectsProperty(HDMaterialProperties.kAffectsMetal);
            if (decalData.affectsSmoothness)
                AddAffectsProperty(HDMaterialProperties.kAffectsSmoothness);
            if (decalData.affectsEmission)
                AddAffectsProperty(HDMaterialProperties.kAffectsEmission);

            // Color mask configuration for writing to the mask map
            AddColorMaskProperty(kDecalColorMask2);
            AddColorMaskProperty(kDecalColorMask3);

            void AddAffectsProperty(string referenceName)
            {
                collector.AddShaderProperty(new BooleanShaderProperty{
                    overrideReferenceName = referenceName,
                    hidden = true,
                    value = true,
                });
            }

            void AddColorMaskProperty(string referenceName)
            {
                collector.AddShaderProperty(new Vector1ShaderProperty{
                    overrideReferenceName = referenceName,
                    floatType = FloatType.Integer,
                    hidden = true,
                });
            }
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Decal = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection
                {
                    { DecalPasses.Projector3RT, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.Projector4RT, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.ProjectorEmissive, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.Mesh3RT, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.Mesh4RT, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.MeshEmissive, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.Preview, new FieldCondition(Fields.IsPreview, true) },
                },
            };
        }
#endregion

#region Passes
        public static class DecalPasses
        {
            // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - s_MaterialDecalNames and s_MaterialDecalSGNames array
            // and DecalSet.InitializeMaterialValues()
            public static PassDescriptor Projector3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = DecalRenderStates.Projector3RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._3RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Projector4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.Projector4RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._4RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor ProjectorEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.ProjectorEmissive,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Mesh3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.Mesh3RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._3RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Mesh4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.Mesh4RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._4RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor MeshEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.MeshEmissive,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Render state overrides
                renderStates = DecalRenderStates.Preview,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };
        }
#endregion

#region BlockMasks
        static class DecalBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.MAOSAlpha,
            };

            public static BlockFieldDescriptor[] FragmentEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Emission,
            };

            public static BlockFieldDescriptor[] FragmentMeshEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.MAOSAlpha,
                BlockFields.SurfaceDescription.Emission,
            };
        }
#endregion

#region RequiredFields
        static class DecalRequiredFields
        {
            public static FieldCollection Mesh = new FieldCollection()
            {
                HDStructFields.AttributesMesh.normalOS,
                HDStructFields.AttributesMesh.tangentOS,
                HDStructFields.AttributesMesh.uv0,
                HDStructFields.FragInputs.tangentToWorld,
                HDStructFields.FragInputs.positionRWS,
                HDStructFields.FragInputs.texCoord0,
            };
        }
#endregion

#region RenderStates
        static class DecalRenderStates
        {
            readonly static string s_DecalColorMask = "ColorMask [_DecalColorMask2] 2 ColorMask [_DecalColorMask3] 3";

            public static RenderStateCollection Projector3RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ColorMask("ColorMask BA 2") },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Projector4RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },

                { RenderState.ColorMask(s_DecalColorMask) }
            };

            public static RenderStateCollection ProjectorEmissive = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection Mesh3RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ColorMask("ColorMask BA 2") },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Mesh4RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
                { RenderState.ColorMask(s_DecalColorMask) }
            };

            public static RenderStateCollection MeshEmissive = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection Preview = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
            };
        }
#endregion

#region Pragmas
        static class DecalPragmas
        {
            public static PragmaCollection Instanced = new PragmaCollection
            {
                { CorePragmas.Basic },
                { Pragma.MultiCompileInstancing },
            };
        }
#endregion

#region Defines
        static class DecalDefines
        {
            static class Descriptors
            {
                public static KeywordDescriptor Decals3RT = new KeywordDescriptor()
                {
                    displayName = "Decals 3RT",
                    referenceName = "DECALS_3RT",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor Decals4RT = new KeywordDescriptor()
                {
                    displayName = "Decals 4RT",
                    referenceName = "DECALS_4RT",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsAlbedo = new KeywordDescriptor()
                {
                    displayName = "Affects Albedo",
                    referenceName = "_MATERIAL_AFFECTS_ALBEDO",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsNormal = new KeywordDescriptor()
                {
                    displayName = "Affects Normal",
                    referenceName = "_MATERIAL_AFFECTS_NORMAL",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsMaskmap = new KeywordDescriptor()
                {
                    displayName = "Affects Maskmap",
                    referenceName = "_MATERIAL_AFFECTS_MASKMAP",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsEmission = new KeywordDescriptor()
                {
                    displayName = "Affects Emission",
                    referenceName = "_MATERIAL_AFFECTS_EMISSION",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };
            }

            public static DefineCollection _3RT = new DefineCollection
            {
                { Descriptors.Decals3RT, 1 },
            };

            public static DefineCollection _4RT = new DefineCollection
            {
                { Descriptors.Decals4RT, 1 },
            };

            public static KeywordCollection Albedo = new KeywordCollection { { Descriptors.AffectsAlbedo, new FieldCondition(AffectsAlbedo, true) } };
            public static KeywordCollection Normal = new KeywordCollection { { Descriptors.AffectsNormal, new FieldCondition(AffectsNormal, true) } };
            public static KeywordCollection Maskmap = new KeywordCollection { { Descriptors.AffectsMaskmap, new FieldCondition(AffectsMaskMap, true) } };
            public static KeywordCollection Emission = new KeywordCollection { { Descriptors.AffectsEmission, new FieldCondition(AffectsEmission, true) } };
        }
#endregion

#region Includes
        static class DecalIncludes
        {
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            const string kDecal = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl";
            const string kPassDecal = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl";

            public static IncludeCollection Default = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { kDecal, IncludeLocation.Pregraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
