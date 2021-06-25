# machine-learning-text-extractor
Program takes in file and uses machine learning to classify file and OCR to extract text from bounding box

User Needs to provide:

1. Image URL
2. Azure Custom Vision Account
  a. ImageClassification Model (prediction key and url)
  b. ObjectDetection Model (prediction key and url)

3. Azure Computer Vision Account - endpoint and subscription key


This program can be modified to suit one's needs

1. Image url is converted to byte array
2. Pass in byte array to model to classify model type
3. If results pass parameters, pass in byte array to second model to find location of desired area
4. If results pass parameters, crop image using bounding box and pass into OCR.
5. OCR extracts text and can be returned as string to use throughout code
