﻿using ProceduralBuildingsGeneration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GeneratorController
{
    public class InputController : IDisposable
    {
        public IGenerationController GenerationControler { get; private set; }
        public ExportController ExportController { get; private set; }
        public VisualizationController VisualizationController { get; private set; }
        public BuildingsViewModel ViewModel { get; private set; }

        protected ModelFormat LatestModelTemporaryFileFormat { get; private set; }
        public InputController()
        {
            GenerationControler = new BuildingsGenerationController();
            ViewModel = new BuildingsViewModel();
            //ViewModel.Grammar = new ViewModelGrammar();
            VisualizationController = new VisualizationController();
            ExportController = new ExportController();
        }

        public bool StartService()
        {
            return VisualizationController.InitializeService();
            //VisualizationController.StartVisualizers(new[] { "WpfVisualizer.exe" });
        }
        public void RequestGenerate()
        {
            VisualizationController.OpenVisualizers();
            m_latestModel = GenerationControler.Generate(ViewModel);
            LatestModelTemporaryFileFormat = ModelFormat.OBJ;
            m_latestModelTemporaryfile?.Dispose();
            ExportController.ExportInStream(m_latestModel, 
                new ExportParameters { ModelFormat = LatestModelTemporaryFileFormat }, 
                out m_latestModelTemporaryfile);
            RequestVisualize();
        }

        public async Task<DateTime> RequestGenerateAsync(Dispatcher uiDispatcher)
        {
            m_latestModel = await GenerationControler.GenerateAsync(ViewModel, uiDispatcher);
            var generationEndTimestamp = DateTime.Now;
            VisualizationController.OpenVisualizers();
            LatestModelTemporaryFileFormat = ModelFormat.OBJ;
            Task.Run(() => m_latestModelTemporaryfile?.Dispose());
            await Task.Run(() => {
                ExportController.ExportInStream(m_latestModel,
                    new ExportParameters { ModelFormat = LatestModelTemporaryFileFormat },
                    out m_latestModelTemporaryfile);
                RequestVisualize();
            });
            return generationEndTimestamp;
        }

        public bool RequestExport(string filepath)
        {
            if (m_latestModel != null)
            {
                var exportResult = ExportController.ExportInFile(m_latestModel, new FileExportParameters
                {
                    FilePath = filepath,
                    ModelFormat = ExportParameters.FormatFromFilePath(filepath)
                });
                return exportResult;
            }
            return false;
        }

        public bool RequestVisualize()
        {
            if (m_latestModelTemporaryfile == null) return false;
            try
            {
                VisualizationController.Visualize(m_latestModelTemporaryfile, LatestModelTemporaryFileFormat);
            }
            catch
            {
                return false;
            }
            return true;
        }


        public void RequestVisualizeAsset(AssetsViewModel assetsViewModel, string assetGroupName, int assetIdx)
        {
            if (!assetsViewModel.CorrectlyParsedManifest) return;
            var asset = assetsViewModel.AssetsLoader
                .AssetGroups[assetGroupName]
                .Assets[assetIdx];
            var assetStream = asset.OpenAssetFile();
            try
            {
                VisualizationController.Visualize(assetStream, asset.FileFormat);
            }
            catch
            {

            }
            assetStream?.Close();
            assetStream?.Dispose();
        }

        public void Dispose()
        {
            VisualizationController.Dispose();
        }

        private Model3d m_latestModel;
        private Stream m_latestModelTemporaryfile;
    }
}
