using System;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;


public class QRScannerAR : MonoBehaviour
{
    private ARCameraManager arCameraManager;
    private IBarcodeReader barcodeReader;
    private bool isScanning = true;
    private GraphManager graphManager;

    // Temporisation pour ne pas surcharger le processeur (ex: 4 analyses par seconde)
    private float intervalleAnalyse = 0.25f;
    private float prochainScanTime = 0f;

    void Start()
    {
        arCameraManager = FindObjectOfType<ARCameraManager>();
        graphManager = FindObjectOfType<GraphManager>();

        barcodeReader = new BarcodeReader
        {
            AutoRotate = false,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        if (arCameraManager == null)
        {
            Debug.LogError("[QR Scanner] ERREUR : Aucun ARCameraManager trouvé dans la scène !");
        }
    }

    void Update()
    {
        // Si le scan est en pause ou qu'il n'y a pas de caméra, on ne fait rien
        if (!isScanning || arCameraManager == null) return;

        // On limite la fréquence d'analyse pour préserver la batterie et la fluidité AR
        if (Time.time < prochainScanTime) return;
        prochainScanTime = Time.time + intervalleAnalyse;

        // MODE AUTONOME : On va chercher directement la dernière image CPU disponible
        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // LOG DE CONFIRMATION : Si tu vois ce message rouge, la communication avec la caméra est établie !
            Debug.LogError("[QR Scanner] Succès ! Image CPU récupérée directement depuis l'Update.");
            AnalyserImageCPU(image);
        }
    }

    private void AnalyserImageCPU(XRCpuImage image)
    {
        using (image)
        {
            // Paramètres de conversion ultra-légers (Résolution divisée par 2)
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.None
            };

            int width = conversionParams.outputDimensions.x;
            int height = conversionParams.outputDimensions.y;

            byte[] buffer = new byte[image.GetConvertedDataSize(conversionParams)];

            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    image.Convert(conversionParams, (IntPtr)ptr, buffer.Length);
                }
            }

            // Génération rapide du tableau Color32 pour ZXing
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                int idx = i * 4;
                pixels[i] = new Color32(buffer[idx], buffer[idx + 1], buffer[idx + 2], buffer[idx + 3]);
            }

            // Décodage de la matrice de pixels
            Result result = barcodeReader.Decode(pixels, width, height);

            if (result != null)
            {
                Debug.LogError("[QR Scanner] MATCH ! QR Code détecté avec succès : " + result.Text);

                isScanning = false; // On coupe le scanner

                if (graphManager != null)
                {
                    graphManager.RecalibratePosition(result.Text);
                }

                // Réactivation automatique après 4 secondes
                Invoke(nameof(ResetScan), 4f);
            }
        }
    }

    void ResetScan()
    {
        isScanning = true;
    }
}