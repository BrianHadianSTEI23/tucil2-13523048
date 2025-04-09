
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
                        Console.WriteLine("Do you want to make a GIF too?");
                        string? GIFRequest = Console.ReadLine();

                        // enter if you want to set the compression target
                        Console.WriteLine("Do you want to make to set compression target?");
                        char? targetRequest = null; 
                        double compressionTarget = 0;

                        // validation compression target request
                        while (targetRequest != 'N' && targetRequest != 'Y')
                        {
                            targetRequest = Convert.ToChar(Console.ReadLine()); // ~broken~
                        } 

                        // enter target request value
                        if (targetRequest == 'Y')
                        {
                            Console.WriteLine("Enter your target value : ");
                            compressionTarget = Convert.ToDouble(Console.ReadLine());
                        } else { // it is guaranteed that it will be 'N'
                            // do nothing
                        }

                        // enter all the config by the user
                        Console.WriteLine("Enter minimum size block : ");
                        int minSize = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("Enter minimum threshold : ");
                        double threshold = Convert.ToDouble(Console.ReadLine());

                        // process the ImageTree
                        BuildTree(root, minSize, minSize, threshold, errorDetectionMethod, compressionTarget);

                        // initialize new image
                        Image<Rgba32> constructImage = new Image<Rgba32>(root.width, root.height);

                        // construct again the image
                        BuildImageFromImageTree(root, ref constructImage);

                        // save the new image with the same format as before
                        image.SaveAsJpeg($"../test/output/rafi_output.{imageFormat.ToLower()}");

                        // success message
                        Console.WriteLine("Your image processed successfully");
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

