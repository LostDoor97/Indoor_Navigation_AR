using System;
using UnityEngine;

namespace IndoorNavigation.QRCode
{
    /// <summary>
    /// Represents decoded QR code data
    /// </summary>
    [System.Serializable]
    public class QRCodeData
    {
        /// <summary>
        /// Raw decoded content from QR code
        /// </summary>
        public string Content;

        /// <summary>
        /// Confidence level of the detection (0-1)
        /// </summary>
        public float Confidence;

        /// <summary>
        /// Timestamp when the QR code was detected
        /// </summary>
        public long DetectionTime;

        /// <summary>
        /// Position of the QR code in the world
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Whether this is a valid marker ID
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Content) && Confidence > 0.7f;

        public QRCodeData()
        {
            DetectionTime = System.DateTime.Now.Ticks;
        }

        public QRCodeData(string content, float confidence, Vector3 position)
        {
            Content = content;
            Confidence = confidence;
            Position = position;
            DetectionTime = System.DateTime.Now.Ticks;
        }
    }
}
