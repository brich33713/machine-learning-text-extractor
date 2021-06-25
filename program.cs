using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace MachineLearning
{
    class Program
    {

        public static void Main()
        {
            //These are the only 3 inputs we will need when converting this to a class to be used throughout code
            string imageLocation = "https://resumecompanion.com/wp-content/uploads/2016/12/Entry-Level-Resume-Template-Aquatic-Blue.png";
            var endpoint = "https://readingcontracts.cognitiveservices.azure.com/";
            var subscriptionKey = "c8b79f70682b4891806adfd0c05ffa19";

            //keys and urls set to variables for better readability
            var predictionKey = "3e42f4f3fa8d4834b9a90f9a6389e745";
            var ObjectDeterctorURL = "https://customvisionvideo.cognitiveservices.azure.com/customvision/v3.0/Prediction/df089820-db3c-434a-b396-3e87efcb1969/detect/iterations/Iteration2/image";
            var ClassificationURL = "https://customvisionvideo.cognitiveservices.azure.com/customvision/v3.0/Prediction/1b47f747-ef32-4337-b1a8-4150f9b8d16d/classify/iterations/Iteration1/image";

            //Creates the ComputerVisionClient which is OCR powered by Azure.
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
            
            //if when documents are uploaded they come in as a url
            byte[] byteData = GetImageAsByteArray(imageLocation);

            //if when documents are uploaded they come in as a file
            //FileStream fileStream = new FileStream(imageLocation, FileMode.Open, FileAccess.Read);
            //BinaryReader binaryReader = new BinaryReader(fileStream);
            //byte[] byteData = binaryReader.ReadBytes((int)fileStream.length);

            //Pass url into so that machine can make a prediction and return results. This is the machine that tell us the type of file
            //TODO: have method return prediction and confidence
            MakePredictionRequest(byteData,predictionKey,ClassificationURL).Wait();

            //if prediction passes parameters and is of right type
            //Pass url into so that machine can make a prediction and return results. This is the machine that will find the area that has text we need to extract.
            //TODO: have method return prediction, confidence, and bounding box
            MakePredictionRequest(byteData,predictionKey,ObjectDeterctorURL).Wait();

                //if prediction passes parameters
                Image img = Image.FromStream(new MemoryStream(byteData));

                //TODO: replace int values with dynamic values returned in bounding box
                MemoryStream croppedImg = CropImage(img, 0, 0, 520, 336);
            
                //Pass in image and get text
                //TODO: have method return text. Values can be used as necessary.
                ReadFileLocal(client, croppedImg).Wait();
        }
        public static async Task MakePredictionRequest(byte[] bytes, string predictorKey, string predictorURL)
        {
            var client = new HttpClient();

            // Request headers - Add("Prediction-Key","<Replace This Value with Prediction Key>")
            client.DefaultRequestHeaders.Add("Prediction-Key", predictorKey);

            // Prediction URL
            string url = predictorURL;

            HttpResponseMessage response;

            

            using (var content = new ByteArrayContent(bytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);

                Console.WriteLine();
                Console.WriteLine("Machine Prediction Results: ");
                Console.WriteLine();

                //TODO: Refactor to make use of response to pull necessary data
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
        }

        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            //probably can be refactored to pass in client, since client will be used throughout the code
            var client = new WebClient();
            
            //converts url to byte array
            return client.DownloadData(imageFilePath);
        }

        public static async Task ReadFileLocal(ComputerVisionClient client, MemoryStream croppedImg)
        {
            // Read text from image
            var textHeaders = await client.ReadInStreamAsync(croppedImg);

            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            Console.WriteLine();
            Console.WriteLine("Text Extracted From Image: ");
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    Console.WriteLine(line.Text);
                }
            }
        }

        public static MemoryStream CropImage(Image source, int x, int y, int width, int height)
        {
            Rectangle crop = new Rectangle(x, y, width, height);

            var bmp = new Bitmap(crop.Width, crop.Height);
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.DrawImage(source, new Rectangle(0, 0, bmp.Width, bmp.Height), crop, GraphicsUnit.Pixel);
            }

            MemoryStream memoryStream = new MemoryStream();

            //Refactor to make ImageFormat equal to original file type and not always default to png
            bmp.Save(memoryStream, ImageFormat.Png);
            
            //Reset Stream
            memoryStream.Position = 0;
            
            return memoryStream;
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }
    }
}
