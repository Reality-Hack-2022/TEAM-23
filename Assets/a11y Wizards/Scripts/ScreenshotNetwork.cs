using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace a11y_Wizards.Scripts
{
    public class ScreenshotNetwork : MonoBehaviour
    {
        [SerializeField] private Camera[] cameras;
        
        //Set your screenshot resolutions
        public int captureWidth = 1920;
        public int captureHeight = 1080;
        // configure with raw, jpg, png, or ppm (simple raw format)
        public enum Format { RAW, JPG, PNG, PPM };
        public Format format = Format.JPG;
        // folder to write output (defaults to data path)
        private string outputFolder;
        // private variables needed for screenshot
        private Rect rect;
        private RenderTexture renderTexture;
        private Texture2D screenShot;

        private bool isProcessing;
       

        /*private void Update() {
        if (Input.GetKeyUp("k"))
            {
                AllyData data = new AllyData();
                data.imgs = new List<string>();

                data.timestamp = DateTime.Now.ToString("yyyyMMddTHHmmss");
                foreach (var camera in cameras)
                {
                    CaptureScreenshot(data, camera);
                }
                WebsocketManager.Instance.SendWebSocketMessage(data.SaveToString());
            }
        }*/

        //Initialize Directory
        private void Start()
        {
            outputFolder = Application.persistentDataPath + "/Screenshots/";
            if(!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                Debug.Log("Save Path will be : " + outputFolder);
            }
        }
        
        private string CreateFileName(string cameraName, int width, int height)
        {
            //timestamp to append to the screenshot filename
            string timestamp = DateTime.Now.ToString("yyyyMMddTHHmmss");
            // use width, height, and timestamp for unique file 
            //var filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", outputFolder, width, height, timestamp, format.ToString().ToLower());
            var filename = $"{timestamp}_{cameraName}_{width}x{height}";
            return filename;
        }

        private AllyData _data;

        public void UpdateAnnotation(string annotationString)
        {
            _data.annotation = annotationString;
        }
        
        public void CaptureData(string annotationString, string timestamp)
        {
            var data = _data = new AllyData();
            
            data.imgs = new List<string>();
            data.annotation = annotationString;
            data.timestamp = timestamp;
            
            foreach (var camera in cameras)
            {
                CaptureScreenshot(data, camera);
            } 
        }

        public void SendData()
        {
            WebsocketManager.Instance.SendWebSocketMessage(_data.SaveToString());
        }

        private void CaptureScreenshot(AllyData data, Camera camera)
        {
            isProcessing = true;
            // create screenshot objects

            // get main camera and render its output into the off-screen render texture created above
            //Camera camera = cameras[0];
                if (renderTexture == null)
                {
                    // creates off-screen render texture to be rendered into
                    rect = new Rect(0, 0, captureWidth, captureHeight);
                    renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
                    screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
                }

                camera.enabled = true;
                camera.targetTexture = renderTexture;
                camera.Render();
                // mark the render texture as active and read the current pixel data into the Texture2D
                RenderTexture.active = renderTexture;
                screenShot.ReadPixels(rect, 0, 0);
                // reset the textures and remove the render texture from the Camera since were done reading the screen data
                camera.targetTexture = null;
                RenderTexture.active = null;
                // get our filename
                string filename = CreateFileName(camera.name, (int)rect.width, (int)rect.height);
                // get file header/data bytes for the specified image format
                byte[] fileHeader = null;
                byte[] fileData = null;
                //Set the format and encode based on it
                if (format == Format.RAW)
                {
                    fileData = screenShot.GetRawTextureData();
                }
                else if (format == Format.PNG)
                {
                    fileData = screenShot.EncodeToPNG();
                }
                else if (format == Format.JPG)
                {
                    fileData = screenShot.EncodeToJPG();
                }
                else //For ppm files
                {
                    // create a file header - ppm files
                    string headerStr = string.Format("P6\n{0} {1}\n255\n", rect.width, rect.height);
                    fileHeader = System.Text.Encoding.ASCII.GetBytes(headerStr);
                    fileData = screenShot.GetRawTextureData();
                }

                var base64 = Convert.ToBase64String(fileData);
                data.imgs.Add(base64);

                // create new thread to offload the saving from the main thread
                new System.Threading.Thread(() =>
                {
                    var file = System.IO.File.Create(filename);
                    if (fileHeader != null)
                    {
                        file.Write(fileHeader, 0, fileHeader.Length);
                    }

                    file.Write(fileData, 0, fileData.Length);
                    file.Close();
                    Debug.Log(string.Format("Screenshot Saved {0}, size {1}, path: {2}", filename, fileData.Length, outputFolder));

                    // Send base64 encoded bytes over websocket to browser
                   // WebsocketManager.Instance.SendWebSocketMessage(Convert.ToBase64String(fileData));
                    isProcessing = false;
                }).Start();
                //Cleanup
                Destroy(renderTexture);
                renderTexture = null;
                screenShot = null;
                camera.enabled = false;
        }
    }
}
