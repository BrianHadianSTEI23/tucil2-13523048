
/*
basic algorithm
1. input the file
2.  read all the pixels and its RGB colors.
3. do mapping of all the pixels and develop error detection method
4. do 

*/ 
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using static ImageTree;

class Program {

    static void Main(string[] args){
        // choose input file
        Console.WriteLine("Enter your photo path : ");
        string? path = Console.ReadLine();

        // path validation
        if (path != null)
        {
            Image<Rgba32> image = Image.Load<Rgba32>(path);
            
            // validation for image
            if (image != null)
            {
                string imageFormat;
                // validation if the file is valid
                if (image.Metadata != null)
                {
                    if (image.Metadata.DecodedImageFormat != null)
                    {
                        imageFormat = image.Metadata.DecodedImageFormat.Name;   
                        // initialize a new imageTree
                        Rgba32 rootRgba = default;
                        image.ProcessPixelRows(_ => {
                            rootRgba = _.GetRowSpan(0)[0];
                        });
                        ImageTree root = new ImageTree(image, 0, 0, image.Width, image.Height, rootRgba, null, null);

                        // debug
                        // printImageTree(root);

                        // enter the choice for error method
                        Console.WriteLine("Enter your preferred method of error detection : ");
                        Console.WriteLine("1. Variance");
                        Console.WriteLine("2. Mean Absolute Deviation");
                        Console.WriteLine("3. Max Pixel Difference");
                        Console.WriteLine("4. Entropy");
                        int errorDetectionMethod = Convert.ToInt16(Console.ReadLine());

                        // enter if you want GIF
                        Console.WriteLine("Do you want to make a GIF too?(Y/N)");
                        bool? GIFRequest = null;

                        // validation GIF request
                        while (GIFRequest != true && GIFRequest != false)
                        {
                            string? response = Console.ReadLine();
                            if (response != null)
                            {
                                if (response[0] == 'Y')
                                {
                                    GIFRequest = true; 
                                } else if (response[0] == 'N') {
                                    GIFRequest = false; 
                                }
                            }  else {
                                Console.WriteLine("Please follow the option command.");
                            }
                        } 

                        // enter if you want to set the compression target
                        Console.WriteLine("Do you want to make to set compression target?(Y/N)");
                        bool? targetRequest = null; 

                        // validation compression target request
                        while (targetRequest != true && targetRequest != false)
                        {
                            string? response = Console.ReadLine();
                            if (response != null)
                            {
                                if (response[0] == 'Y')
                                {
                                    targetRequest = true; 
                                } else if (response[0] == 'N') {
                                    targetRequest = false; 
                                }
                            } else {
                                Console.WriteLine("Please follow the option command.");
                            }
                        } 

                        // enter target request value
                        double compressionTarget = 0;
                        if (targetRequest == true)
                        {
                            Console.WriteLine("Enter your target value : ");
                            try
                            {
                                compressionTarget = Convert.ToDouble(Console.ReadLine());
                            }
                            catch (System.Exception)
                            {
                                Console.WriteLine("Wrong type target value. Exiting");
                                throw;
                            }
                        } else { // it is guaranteed that it will be 'N'
                            // do nothing
                        }

                        // enter all the config by the user
                        Console.WriteLine("Enter minimum size block : ");
                        int minSize = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("Enter minimum threshold : ");
                        double threshold = Convert.ToDouble(Console.ReadLine());

                        // process the ImageTree
                        BuildTree(ref root, minSize, minSize, threshold, errorDetectionMethod, compressionTarget);

                        // debug
                        // printImageTree(root);

                        // initialize new image
                        Image<Rgba32> constructImage = new Image<Rgba32>(root.width, root.height);
                        
                        // construct again the image
                        BuildImageFromImageTree(root,ref constructImage);

                        // construct GIF (if requested)
                        // Image<Rgba32> constructGIF = new Image<Rgba32>(root.width, root.height);
                        // if (GIFRequest == true)
                        // {
                        //     BuildImageGIFFromImageTree(root, GIFRequest, ref constructGIF);   
                        // }

                        // save the new image + GIF (if requested) with the same format as before
                        string[] pathArray = path.Split('\\', '.'); // partition the input path
                        if (imageFormat == "JPEG")
                        {
                            constructImage.SaveAsJpeg($"../test/output/{pathArray[pathArray.Length - 2]}_output.jpeg" ); 
                        } else if (imageFormat == "PNG"){
                            constructImage.SaveAsPng($"../test/output/{pathArray[pathArray.Length - 2]}_output.png" ); 
                        } else if (imageFormat == "PBM") {
                            constructImage.SaveAsPbm($"../test/output/{pathArray[pathArray.Length - 2]}_output.pbm" ); 
                        } else if (imageFormat == "QOI"){
                            constructImage.SaveAsQoi($"../test/output/{pathArray[pathArray.Length - 2]}_output.qoi" ); 
                        } else if (imageFormat == "TGA"){
                            constructImage.SaveAsTga($"../test/output/{pathArray[pathArray.Length - 2]}_output.tga" ); 
                        } else if (imageFormat == "TIFF"){
                            constructImage.SaveAsTiff($"../test/output/{pathArray[pathArray.Length - 2]}_output.tiff" ); 
                        }


                        // success message
                        Console.WriteLine($"Your image processed successfully and saved at ../test/output/{pathArray[pathArray.Length - 2]}_output");
                        // if (GIFRequest == true)
                        // {
                        //     // Remove the default empty first frame (if not replaced)
                        //     // constructGIF.Frames.RemoveFrame(0);

                        //     // success message
                        //     constructGIF.SaveAsGif($"../test/output/{pathArray[pathArray.Length - 2]}_output_gif.gif");
                        //     Console.WriteLine($"Your GIF image processed successfully and saved at ../test/output/{pathArray[pathArray.Length - 2]}_output_gif");
                        // }
                    } else {
                    Console.WriteLine("No image format available");
                    }
                } else {
                    Console.WriteLine("No metadata available");
                }

            } else {
                Console.WriteLine("No file like so.");
            }
        } else {
            Console.WriteLine("File not found");
        }
        



    }

}

