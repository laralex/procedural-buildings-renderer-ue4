﻿using g3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProceduralBuildingsGeneration
{
    public class BuildingsModelsGenerator : IProceduralModelsGenerator
    {
        public Model3d GenerateModel(GenerationParameters parameters)
        {
            var buildingParams = parameters as BuildingsGenerationParameters;
            
            var assetsMeshes = new Dictionary<Asset, DMesh3>();

            LoadAssetsAsMeshes(buildingParams.DoorsAssets, 
                buildingParams.DoorAssetTrianglesLimit,
                1.0 / buildingParams.AssetsScaleModifier, assetsMeshes);
            LoadAssetsAsMeshes(buildingParams.WindowsAssets,
                buildingParams.WindowAssetTrianglesLimit,
                1.0 / buildingParams.AssetsScaleModifier, assetsMeshes);
            
            // make grammar
            var grammarController = new BuildingsGrammarController(buildingParams.RandomGenerator, assetsMeshes);
            var buildingWord = grammarController.TransformWordRepeatedly(parameters, 10);

            // build model
            var buildingMeshes = MakeMeshFromGrammar(buildingWord);
            if (!buildingMeshes[0].CheckValidity()) throw new Exception("Generated mesh is invalid");


            foreach (var doorAsset in buildingParams.DoorsAssets)
                doorAsset.CloseAssetFile();
            foreach (var windowAsset in buildingParams.WindowsAssets)
                windowAsset.CloseAssetFile();
            assetsMeshes.Clear();

            //Reducer r = new Reducer(mesh);
            //r.ReduceToTriangleCount(1000);

            return new Model3d { Mesh = buildingMeshes };
        }

        private static void LoadAssetsAsMeshes(IList<Asset> assets, int trianglesLimit, double scale, Dictionary<Asset, DMesh3> destination)
        {
            if (destination == null) return;
            var meshBuilder = new DMesh3Builder() { NonManifoldTriBehavior = DMesh3Builder.AddTriangleFailBehaviors.DiscardTriangle };
            var objReader = new OBJFormatReader();
            var reader = new StandardMeshReader() { MeshBuilder = meshBuilder, ReadInvariantCulture = true  };
            //reader.AddFormatHandler(objReader);
            foreach (var asset in assets)
            {
                //var isMeshLoaded = objReader.ReadFile(asset.OpenAssetFile(), meshBuilder, null, new ParsingMessagesHandler((s, o) => {; }));
                var isMeshLoaded = reader.Read(asset.OpenAssetFile(), asset.FileFormat.ToString(), ReadOptions.Defaults);
                if (isMeshLoaded.code == IOCode.Ok)
                {
                    var mesh = meshBuilder.Meshes.Last();
                    Reducer r = new Reducer(mesh)
                    {
                        PreserveBoundaryShape = true,
                    };
                    r.ReduceToTriangleCount(trianglesLimit);
                    MeshTransforms.Scale(mesh, scale);
                    destination[asset] = mesh;
                }
            }   
        }
        private static IList<DMesh3> MakeMeshFromGrammar(GrammarNode buildingWord)
        {
            var mesh = new DMesh3(MeshComponents.VertexNormals | MeshComponents.FaceGroups);
            var builder = new DMesh3Builder() {
                Meshes = { mesh },
                DuplicateTriBehavior = DMesh3Builder.AddTriangleFailBehaviors.DiscardTriangle,
                NonManifoldTriBehavior = DMesh3Builder.AddTriangleFailBehaviors.DiscardTriangle,
            };
            builder.SetActiveMesh(0);

            var tasksToWait = new List<Task>();
            ApplyNodesRecursively(builder, buildingWord, tasksToWait);
            Task.WaitAll(tasksToWait.ToArray());
            return builder.Meshes;
        }

        private static void ApplyNodesRecursively(DMesh3Builder meshBuilder, GrammarNode currentNode, List<Task> tasksToWait)
        {
            if (currentNode == null) return;
            currentNode.BuildOnMesh(meshBuilder);
            if (currentNode is WindowNode) tasksToWait.Add((currentNode as WindowNode).BuildingTask);
            foreach (var child in currentNode.Subnodes)
            {
                ApplyNodesRecursively(meshBuilder, child, tasksToWait);
            }
        }
    }
}