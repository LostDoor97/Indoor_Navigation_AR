using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;

namespace IndoorNavigation.QRCode
{
    /// <summary>
    /// Handles QR code detection and processing using ARCore camera
    /// Note: Requires ZXing.Net library for QR decoding
    /// </summary>
    public class QRCodeScanner : MonoBehaviour
    {
        [Header("AR Setup")]
        [SerializeField] private ARCameraManager m_CameraManager;
        [SerializeField] private ARTrackedImageManager m_ImageManager;

        [Header("Detection Settings")]
        [SerializeField] private float m_ScanUpdateRate = 0.5f;
        [SerializeField] private float m_MinConfidenceThreshold = 0.7f;
        [SerializeField] private float m_MaxScanDistance = 5f;

        // Events
        public delegate void QRCodeDetectedDelegate(QRCodeData qrData);
        public event QRCodeDetectedDelegate OnQRCodeDetected;

        public delegate void ScanStatusChangeDelegate(string status);
        public event ScanStatusChangeDelegate OnScanStatusChanged;

        private Texture2D m_CameraTexture;
        private float m_LastScanTime;
        private bool m_IsScanning;
        private Queue<QRCodeData> m_DetectedCodes;

        private void OnEnable()
        {
            m_DetectedCodes = new Queue<QRCodeData>();
            m_LastScanTime = Time.time;

            if (m_ImageManager != null)
            {
                m_ImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            }
        }

        private void OnDisable()
        {
            if (m_ImageManager != null)
            {
                m_ImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
            }
        }

        private void Update()
        {
            if (!m_IsScanning)
                return;

            if (Time.time - m_LastScanTime >= m_ScanUpdateRate)
            {
                AttemptQRCodeScan();
                m_LastScanTime = Time.time;
            }
        }

        /// <summary>
        /// Start scanning for QR codes
        /// </summary>
        public void StartScanning()
        {
            if (m_CameraManager == null)
            {
                Debug.LogError("[QR Code Scanner] ARCameraManager not assigned");
                return;
            }

            m_IsScanning = true;
            m_LastScanTime = Time.time;
            OnScanStatusChanged?.Invoke("Scanning...");
            Debug.Log("[QR Code Scanner] Started scanning");
        }

        /// <summary>
        /// Stop scanning for QR codes
        /// </summary>
        public void StopScanning()
        {
            m_IsScanning = false;
            OnScanStatusChanged?.Invoke("Stopped");
            Debug.Log("[QR Code Scanner] Stopped scanning");
        }

        /// <summary>
        /// Attempt to detect QR code from camera
        /// </summary>
        private void AttemptQRCodeScan()
        {
            try
            {
                if (!m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                {
                    return;
                }

                using (image)
                {
                    var conversionParams = new XRCpuImage.ConversionParams
                    {
                        inputRect = new RectInt(0, 0, image.width, image.height),
                        outputDimensions = new Vector2Int(image.width, image.height),
                        outputFormat = TextureFormat.RGB24,
                        transformation = XRCpuImage.Transformation.MirrorY
                    };

                    int size = image.width * image.height * 3;
                    var buffer = new NativeArray<byte>(size, Allocator.Temp);

                    m_CameraTexture = new Texture2D(
                        conversionParams.outputDimensions.x,
                        conversionParams.outputDimensions.y,
                        TextureFormat.RGB24,
                        false
                    );

                    image.Convert(conversionParams, buffer);
                    m_CameraTexture.LoadRawTextureData(buffer);
                    m_CameraTexture.Apply();
                    buffer.Dispose();

                    ProcessCameraFrame(m_CameraTexture);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QR Code Scanner] Error during scan: {ex.Message}");
            }
        }

        /// <summary>
        /// Process a single camera frame for QR codes
        /// </summary>
        private void ProcessCameraFrame(Texture2D frameTexture)
        {
            if (frameTexture == null)
                return;

            // Placeholder for ZXing.Net QR decoding
            // In production, use: ZXing.BarcodeReader to scan the texture
            // Example:
            // var reader = new ZXing.BarcodeReader();
            // var result = reader.Decode(frameTexture);

            // For now, we'll simulate detection from tracked images
        }

        /// <summary>
        /// Called when ARKit tracks images
        /// </summary>
        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
        {
            foreach (var trackedImage in args.added)
            {
                ProcessTrackedImage(trackedImage);
            }

            foreach (var trackedImage in args.updated)
            {
                if (trackedImage.trackingState == TrackingState.Tracking)
                {
                    ProcessTrackedImage(trackedImage);
                }
            }
        }

        /// <summary>
        /// Process a tracked image as a QR code marker
        /// </summary>
        private void ProcessTrackedImage(ARTrackedImage trackedImage)
        {
            if (Camera.main == null) return;

            if (Vector3.Distance(Camera.main.transform.position, trackedImage.transform.position) > m_MaxScanDistance)
                return;

            var qrData = new QRCodeData(
                trackedImage.referenceImage.name,
                1f,
                trackedImage.transform.position
            );

            if (qrData.IsValid)
            {
                m_DetectedCodes.Enqueue(qrData);
                OnQRCodeDetected?.Invoke(qrData);
                OnScanStatusChanged?.Invoke($"Detected: {qrData.Content}");
                Debug.Log($"[QR Code Scanner] Detected marker: {qrData.Content}");
            }
        }

        /// <summary>
        /// Get the most recent detected QR code
        /// </summary>
        public QRCodeData GetLatestDetectedCode()
        {
            if (m_DetectedCodes.Count > 0)
            {
                return m_DetectedCodes.Dequeue();
            }
            return null;
        }

        /// <summary>
        /// Clear all detected codes from queue
        /// </summary>
        public void ClearDetectedCodes()
        {
            m_DetectedCodes.Clear();
        }

        /// <summary>
        /// Check if currently scanning
        /// </summary>
        public bool IsScanning => m_IsScanning;
    }
}
