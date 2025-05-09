﻿using System.Data;
using Microsoft.VisualBasic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ChildTree {
    public ImageTree? LeftTop;  // may be null  
    public ImageTree? RightTop;    
    public ImageTree? LeftBottom;    
    public ImageTree? RightBottom;  

    // constructor
    public ChildTree (){}
}

public class ImageTree {
    public Image<Rgba32>? image; // it may be null
    public int x;
    public int y;
    public int width; // this is width, not the end point of x of the current imageTree
    public int height; // this is height, not the end point of y of the current imageTree
    public Rgba32 rgba;
    public ImageTree? parent; // for root, this is null
    public ChildTree? child; // for leaf, this equal to null

    // constructor
    public ImageTree(Image<Rgba32> I, int X, int Y, int W, int H, Rgba32 COL, ImageTree? P, ChildTree? C) {
        this.image = I;
        this.x = X;
        this.y = Y;
        this.width = W;
        this.height = H;
        this.rgba = COL;
        this.parent = P;
        this.child = C;
    }

    // basic operations
    // get total simpul 
    public int getTotalNode(ImageTree root, ref int currTotalNode){
        // default : currtotalnode = 0
        if (root.child != null)
        {
            currTotalNode++;
            if (root.child.LeftBottom != null && root.child.RightBottom != null && root.child.LeftTop != null && root.child.RightTop != null)
            {
                getTotalNode(root.child.LeftBottom, ref currTotalNode);
                getTotalNode(root.child.RightBottom, ref currTotalNode);
                getTotalNode(root.child.LeftTop, ref currTotalNode);
                getTotalNode(root.child.RightTop, ref currTotalNode);
            }
        }
        return currTotalNode;
    }

    // get depth of simpul
    public int getImageTreeDepth(ImageTree root){
        if (root == null || root.child == null)
        {
            return 1;
        }

        // count for each branchchild
        int lt = 0, rt = 0, lb = 0, rb = 0;
        if (root.child.LeftBottom != null && root.child.RightBottom != null && root.child.LeftTop != null && root.child.RightTop != null)
        {
            lt = getImageTreeDepth(root.child.LeftTop);
            rt = getImageTreeDepth(root.child.RightTop);
            lb = getImageTreeDepth(root.child.LeftBottom);
            rb = getImageTreeDepth(root.child.RightBottom);
            
        }

        return 1 + Math.Max(Math.Max(lt, rt), Math.Max(lb, rb));
    }
    
    // operations
    // build the recursive tree
    public static void BuildTree(ref ImageTree root, int minWidth, int minHeight, double threshold, int? errorDetectionMethod, ref ImageTree top) {
        // check if it's still possible to be partitioned
        if ((root.width / 4) >= minWidth && (root.height / 4) >= minHeight)
        {
            // debug

            double currThreshold = 0;
            switch (errorDetectionMethod) {
                case 1 : 
                    currThreshold = ImageTreeVariance(root);
                    break;
                case 2 : 
                    currThreshold = ImageTreeMeanAbsoluteDeviation(root);
                    break;
                    
                case 3 : 
                    currThreshold = MaxPixelDifferenceImageTree(root);
                    break;

                case 4 : 
                    currThreshold = EntropyImageTree(root);
                    break;
                case 5 : 
                    currThreshold = SSIMImageTree(root, ref top);
                    break;
                default : 
                    Console.WriteLine("No method as such. Exiting...");
                    break;
            }

            // debug
            // Console.WriteLine($"Current threshold : {currThreshold}");

            // do divide and conquer
            if (errorDetectionMethod != null)
            {
                if (currThreshold > threshold) // still need to be partitioned
                {
                    // initialize every child tree
                    if (root.image != null)
                    {    

                        // create root copy to be referenced for the lambda function
                        ImageTree cloneRoot = root;

                        /*
                        if the image has odd size of pixels, then the guideline is to left and top frame will always have the fewer ones when pixels is divided into two, but to right and bottom will have the remaining ones
                        */
                        int leftWidth = root.width / 2;
                        int rightWidth = root.width - leftWidth;
                        int topHeight = root.height / 2;
                        int bottomHeight = root.height - topHeight;

                        // clamp position for boundary index
                        int rightX = Math.Min(cloneRoot.x + leftWidth, root.image.Width - 1);
                        int bottomY = Math.Min(cloneRoot.y + topHeight, root.image.Height - 1);

                        // generate child for binding all the child to the root
                        ChildTree childTree = new ChildTree();

                        // left top
                        ImageTree LeftTop = new ImageTree(cloneRoot.image, cloneRoot.x, cloneRoot.y, leftWidth, topHeight, cloneRoot.rgba, cloneRoot, null);
                        childTree.LeftTop = LeftTop;
                        BuildTree(ref LeftTop, minWidth, minHeight, threshold, errorDetectionMethod, ref top);

                        // right top
                        Rgba32 rgbaRightTop = default;
                        root.image.ProcessPixelRows(_ => { // get the rgba of the current (x, y)
                            rgbaRightTop = _.GetRowSpan(cloneRoot.y)[cloneRoot.x + leftWidth];
                        });
                        ImageTree RightTop = new ImageTree(cloneRoot.image, cloneRoot.x + leftWidth, cloneRoot.y, rightWidth, topHeight, rgbaRightTop, cloneRoot, null);
                        childTree.RightTop = RightTop;
                        BuildTree(ref RightTop, minWidth, minHeight, threshold, errorDetectionMethod, ref top);

                        // Left bottom
                        Rgba32 rgbaLeftBottom = default;
                        root.image.ProcessPixelRows(_ => { // get the rgba of the current (x, y)
                            rgbaLeftBottom = _.GetRowSpan(cloneRoot.y + topHeight)[cloneRoot.x];
                        });
                        ImageTree LeftBottom = new ImageTree(cloneRoot.image, cloneRoot.x, cloneRoot.y + topHeight, leftWidth, bottomHeight, rgbaLeftBottom, cloneRoot, null);
                        childTree.LeftBottom = LeftBottom;
                        BuildTree(ref LeftBottom, minWidth, minHeight, threshold, errorDetectionMethod, ref top);

                        // Right Bottom
                        Rgba32 rgbaRightBottom = default;
                        root.image.ProcessPixelRows(_ => { // get the rgba of the current (x, y)
                            rgbaRightBottom = _.GetRowSpan(cloneRoot.y + topHeight)[cloneRoot.x + leftWidth];
                        });
                        ImageTree RightBottom = new ImageTree(cloneRoot.image, cloneRoot.x + leftWidth, cloneRoot.y + topHeight, rightWidth, bottomHeight, rgbaRightBottom, cloneRoot, null);
                        childTree.RightBottom = RightBottom;
                        BuildTree(ref RightBottom, minWidth, minHeight, threshold, errorDetectionMethod, ref top);

                        // bind all the child
                        root.child = childTree;
                    }
                } else {
                    NormalizeImageTree(root);
                }
            }
        } else {
            if (errorDetectionMethod != null)
            {
                // debug
                // Console.WriteLine("");
                // printImageTree(root);
                NormalizeImageTree(root);
            }
        }
        return ;
    }

    // build image from processed imageTree
    public static void BuildImageFromImageTree(ImageTree? root, ref Image<Rgba32> constructImage){
        // validation
        if (root.child != null)
        {
            // build the rest
            BuildImageFromImageTree(root.child.LeftBottom, ref constructImage);
            BuildImageFromImageTree(root.child.RightBottom,  ref constructImage);
            BuildImageFromImageTree(root.child.LeftTop, ref constructImage);
            BuildImageFromImageTree(root.child.RightTop, ref constructImage);
        } else {
            constructImage.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        // set new value
                        // Console.WriteLine($"Drawing area at ({x},{y}) with size {root.width}x{root.height} and color {root.rgba}"); // debug
                        row[x] = new Rgba32(root.rgba.R, root.rgba.G, root.rgba.B, root.rgba.A);
                    }
                }
            });

        }
        return ;
    }

    // build image gif
    public static void BuildImageGIFFromImageTree(ImageTree? root, bool? GIFRequest, ref Image<Rgba32> GIFConstructImage, ref int currGIFFrame){
        // validate if the gif is valid 
        if (GIFRequest == true) 
        {
            if (root != null)
            {
                // set frame
                Image<Rgba32> frame = new Image<Rgba32>(root.image.Width, root.image.Height);

                // develpo frame
                frame.ProcessPixelRows(_ => {
                    for (int y = root.y; y < root.y + root.height; y++)
                    {
                        var row = _.GetRowSpan(y);
                        for (int x = root.x; x < root.x + root.width; x++)
                        {
                            // set new value
                            row[x] = new Rgba32(root.rgba.R, root.rgba.G, root.rgba.B, root.rgba.A);
                        }
                    }
                });

                // set the frame time to be 0.1 s
                frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 2;

                // add the frame to the gif construct image
                GIFConstructImage.Frames.AddFrame(frame.Frames.RootFrame);   

                // increment the currGIFFrame until max GIF Frame
                currGIFFrame++;

                if (currGIFFrame < 10000) // 10000 is max GIF Frame (set)
                {
                    if (root.child != null)
                    {
                        BuildImageGIFFromImageTree(root.child.LeftBottom, GIFRequest, ref GIFConstructImage, ref currGIFFrame);
                        BuildImageGIFFromImageTree(root.child.RightBottom, GIFRequest, ref GIFConstructImage, ref currGIFFrame);
                        BuildImageGIFFromImageTree(root.child.LeftTop, GIFRequest, ref GIFConstructImage, ref currGIFFrame);
                        BuildImageGIFFromImageTree(root.child.RightTop, GIFRequest, ref GIFConstructImage, ref currGIFFrame);
                    }                
                    
                }

            }
        }
        return ;
    }
    // bonus ssim : SSIMImageTree
    public static double SSIMImageTree(ImageTree root, ref ImageTree top){ // `top` variable is a the toppest ImageTree
        const double K1 = 0.01;
        const double K2 = 0.03;
        const double L = 255;
        const double C1 = K1 * K1 * L * L;
        const double C2 = K2 * K2 * L * L;
        const double WRed = 0.2126;
        const double WGreen = 0.7152;
        const double WBlue = 0.0722;

        // mean of current image of each color channel
        double meanColorChannelValueRed = MeanChannelColorValue(root, 'R');
        double meanColorChannelValueGreen = MeanChannelColorValue(root, 'G');
        double meanColorChannelValueBlue = MeanChannelColorValue(root, 'B');
        // Console.WriteLine(meanColorChannelValueRed);
        // Console.WriteLine(meanColorChannelValueGreen);
        // Console.WriteLine(meanColorChannelValueBlue);

        // mean of original image of each color
        double meanColorChannelValueOriginalRed= MeanChannelColorValue(top, 'R');
        double meanColorChannelValueOriginalGreen= MeanChannelColorValue(top, 'G');
        double meanColorChannelValueOriginalBlue= MeanChannelColorValue(top, 'B');
        // Console.WriteLine(meanColorChannelValueOriginalRed);
        // Console.WriteLine(meanColorChannelValueOriginalGreen);
        // Console.WriteLine(meanColorChannelValueOriginalBlue);

        // variance of current image of each color
        double varianceRed = VarianceImageTreeColorChannel(root, 'R');
        double varianceGreen = VarianceImageTreeColorChannel(root, 'G');
        double varianceBlue = VarianceImageTreeColorChannel(root, 'B');
        // Console.WriteLine(varianceRed);
        // Console.WriteLine(varianceGreen);
        // Console.WriteLine(varianceBlue);

        // variance of original image of each color channel
        double varianceRedOriginal = VarianceImageTreeColorChannel(top, 'R');
        double varianceGreenOriginal = VarianceImageTreeColorChannel(top, 'G');
        double varianceBlueOriginal = VarianceImageTreeColorChannel(top, 'B');
        // Console.WriteLine(varianceRedOriginal);
        // Console.WriteLine(varianceGreenOriginal);
        // Console.WriteLine(varianceBlueOriginal);

        // covariance of each color channel for both image
        double covarianceRed = SSIMCovarianceColorChannelImageTree(root, ref top, 'R');
        double covarianceGreen = SSIMCovarianceColorChannelImageTree(root, ref top, 'G');
        double covarianceBlue = SSIMCovarianceColorChannelImageTree(root, ref top, 'B');
        // Console.WriteLine(covarianceRed);
        // Console.WriteLine(covarianceGreen);
        // Console.WriteLine(covarianceBlue);

        // numerator
        double numeratorRed = ((2 * meanColorChannelValueOriginalRed * meanColorChannelValueRed) + C1) * ((2 * covarianceRed) + C2);
        double numeratorGreen = ((2 * meanColorChannelValueOriginalGreen * meanColorChannelValueGreen) + C1) * ((2 * covarianceGreen) + C2);
        double numeratorBlue = ((2 * meanColorChannelValueOriginalBlue * meanColorChannelValueBlue) + C1) * ((2 * covarianceBlue) + C2);

        // denumerator
        double denumeratorRed = (((meanColorChannelValueOriginalRed * meanColorChannelValueOriginalRed) + (meanColorChannelValueRed * meanColorChannelValueRed)) + C1) * (varianceRed + varianceRedOriginal + C2);
        double denumeratorGreen = (((meanColorChannelValueOriginalGreen * meanColorChannelValueOriginalGreen) + (meanColorChannelValueGreen * meanColorChannelValueGreen)) + C1) * (varianceGreen + varianceGreenOriginal + C2);
        double denumeratorBlue = (((meanColorChannelValueOriginalBlue * meanColorChannelValueOriginalBlue) + (meanColorChannelValueBlue * meanColorChannelValueBlue)) + C1) * (varianceBlue + varianceBlueOriginal + C2);

        // SSIM for each color channel
        double SSIMRed = numeratorRed / denumeratorRed;
        double SSIMGreen = numeratorGreen / denumeratorGreen;
        double SSIMBlue = numeratorBlue / denumeratorBlue;

        // debug
        Console.WriteLine("Res : " + (WRed * SSIMRed) + (WGreen * SSIMGreen) + (WBlue * SSIMBlue));
        return (WRed * SSIMRed) + (WGreen * SSIMGreen) + (WBlue * SSIMBlue);
    }

    // count variance of four of the child : variance
    public static double ImageTreeVariance(ImageTree root) {
        // assumption : root is always safe to be partitioned

        // count the variance of each rgb
        double RedVariance = VarianceImageTreeColorChannel(root, 'R');
        double BlueVariance = VarianceImageTreeColorChannel(root, 'B');
        double GreenVariance = VarianceImageTreeColorChannel(root, 'G');

        // debug
        // Console.WriteLine($"Variance : {(RedVariance + BlueVariance + GreenVariance) / 3}");
        
        return (RedVariance + BlueVariance + GreenVariance) / 3;
    }

    public static double VarianceImageTreeColorChannel(ImageTree root, char colorChannel){
        // count the mean of rgb value
        double differenceColorChannelSquared = 0;
        double colorChannelMean = MeanChannelColorValue(root, colorChannel);
        
        // count all the pixel color channel and do variance
        if (root.image != null) // error handling
        {
            root.image.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        var pixel = row[x];
                        if (colorChannel == 'R')
                        {
                            differenceColorChannelSquared += Math.Pow(pixel.R - colorChannelMean, 2);
                        } else if (colorChannel == 'G') {
                            differenceColorChannelSquared += Math.Pow(pixel.B - colorChannelMean, 2);
                        } else if (colorChannel == 'B') {
                            differenceColorChannelSquared += Math.Pow(pixel.B - colorChannelMean, 2);
                        }
                    }
                }
            });
        } else {
            Console.WriteLine("Root image is empty");
        }


        return differenceColorChannelSquared / (root.width * root.height);
    }


    // bonus ssim : covariance of two image of a certain color channel
    public static double SSIMCovarianceColorChannelImageTree(ImageTree root, ref ImageTree top, char colorChannel){
        // copy top
        ImageTree cloneTop = top;
        
        // covariance is compared with the original image
        double colorChannelMean = MeanChannelColorValue(root, colorChannel);
        double colorChannelMeanOriginal = MeanChannelColorValue(cloneTop, colorChannel);

        // count covariance
        double covarianceValue = 0;
        root.image.ProcessPixelRows(_ => {
            for (int y = root.y; y < root.y + root.height; y++)
            {
                var row = _.GetRowSpan(y);
                for (int x = root.x; x < root.x + root.width; x++)
                {
                    var pixel = row[x];
                    if (colorChannel == 'R') {
                        covarianceValue += (pixel.R - colorChannelMeanOriginal) * (cloneTop.rgba.R - colorChannelMeanOriginal);
                    } else if (colorChannel == 'B') {
                        covarianceValue += (pixel.B - colorChannelMeanOriginal) * (cloneTop.rgba.B - colorChannelMeanOriginal);
                    } else if (colorChannel == 'G'){
                        covarianceValue += (pixel.G - colorChannelMeanOriginal) * (cloneTop.rgba.G - colorChannelMeanOriginal);
                    } 
                }
            }
        });

        return covarianceValue / ((root.width * root.height) - 1);
    }

    public static double MeanChannelColorValue(ImageTree root, char colorChannel) {
        int totalValueColorChannel = 0;

        if (root.image != null)
        {
            root.image.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    // Console.WriteLine(y); // debug
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        var pixel = row[x];
                        if (colorChannel == 'R') {
                            totalValueColorChannel += pixel.R;
                        } else if (colorChannel == 'B') {
                            totalValueColorChannel += pixel.B;
                        } else if (colorChannel == 'G'){
                            totalValueColorChannel += pixel.G;
                        }
                    }
                }
            });
        } else {
            Console.WriteLine("Root image is empty");
        }

        return totalValueColorChannel / (root.width * root.height);
    }

    // count the variance of the four child : Mean Absolute Deviation
    public static double ImageTreeMeanAbsoluteDeviation(ImageTree root){

        // get the MAD of each color channel
        double MADRed = MeanAbsoluteDeviationImageTreeColorChannel(root, 'R');
        double MADGreen = MeanAbsoluteDeviationImageTreeColorChannel(root, 'G');
        double MADBlue = MeanAbsoluteDeviationImageTreeColorChannel(root, 'B');

        // debug
        // Console.WriteLine($"MAD : {(MADRed + MADBlue + MADGreen) / 3}");

        return (MADRed + MADBlue + MADGreen) / 3;
    }

    // count each MAD of each color channel
    public static double MeanAbsoluteDeviationImageTreeColorChannel(ImageTree root, char colorChannel){
        double AbsoluteDeviation = 0;
        double colorChannelMean = MeanChannelColorValue(root, colorChannel);

        if (root.image != null)
        {
            root.image.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        var pixel = row[x];
                        if (colorChannel == 'R')
                        {
                            AbsoluteDeviation += Math.Abs(pixel.R - colorChannelMean);
                        } else if (colorChannel == 'G'){
                            AbsoluteDeviation += Math.Abs(pixel.G - colorChannelMean);
                        } else if (colorChannel == 'B') {
                            AbsoluteDeviation += Math.Abs(pixel.B - colorChannelMean);
                        }
                    }
                }
            });
        } else {
            Console.WriteLine("Root image null");
        }

        return AbsoluteDeviation / (root.width * root.height);
    }

    // count the variance of the image : Max Pixel Difference
    public static double MaxPixelDifferenceImageTree(ImageTree root){
        // difference of each color channel
        int diffRed = MaxValueImageTreeColorChannel(root, 'R');
        int diffGreen = MaxValueImageTreeColorChannel(root, 'G');
        int diffBlue = MaxValueImageTreeColorChannel(root, 'B');

        // debug
        // Console.WriteLine($"Max Pixel Difference : {(diffRed + diffGreen + diffBlue) / 3}");

        return (diffRed + diffGreen + diffBlue) / 3;
    }

    // search the max value of certain color channel
    public static int MaxValueImageTreeColorChannel(ImageTree root, char colorChannel) {
        int MaxValColorChannel = 255; // default is to max rgb value

        // validation
        if (root.image != null) {
            
            root.image.ProcessPixelRows(_ => {
                    
                if (colorChannel == 'R')
                {
                    MaxValColorChannel = _.GetRowSpan(root.y)[root.x].R;
                    for (int y = root.y; y < root.y + root.height; y++)
                    {
                        var row = _.GetRowSpan(y);
                        for (int x = root.x; x < root.x + root.width; x++)
                        {
                            var pixel = row[x];
                            if (pixel.R > MaxValColorChannel)
                            {
                                MaxValColorChannel = pixel.R;
                            }
                        }
                    }
                } else if (colorChannel == 'G') { // color channel is green
                    MaxValColorChannel = _.GetRowSpan(root.y)[root.x].G;
                    for (int y = root.y; y < root.y + root.height; y++)
                    {
                        var row = _.GetRowSpan(y);
                        for (int x = root.x; x < root.x + root.width; x++)
                        {
                            var pixel = row[x];
                            if (pixel.G > MaxValColorChannel)
                            {
                                MaxValColorChannel = pixel.G;
                            }
                        }
                    }
                } else if (colorChannel == 'B') { // if blue
                        MaxValColorChannel = _.GetRowSpan(root.y)[root.x].B;
                        for (int y = root.y; y < root.y + root.height; y++)
                        {
                            var row = _.GetRowSpan(y);
                            for (int x = root.x; x < root.x + root.width; x++)
                            {
                                var pixel = row[x];
                                if (pixel.B > MaxValColorChannel)
                                {
                                    MaxValColorChannel = pixel.B;
                                }
                            }
                        }
                }
            });
        }

        return MaxValColorChannel;
    }

    // search the min value of certain color channel
    public static int MinValueImageTreeColorChannel(ImageTree root, char colorChannel) {
        int MinValColorChannel = 0;

        // validation
        if (root.image != null)
        {
            
            root.image.ProcessPixelRows(_ => {
                    
                if (colorChannel == 'R')
                {
                    MinValColorChannel = _.GetRowSpan(root.y)[root.x].R;
                    for (int y = root.y; y < root.y + root.height; y++)
                    {
                        var row = _.GetRowSpan(y);
                        for (int x = root.x; x < root.x + root.width; x++)
                        {
                            var pixel = row[x];
                            if (pixel.R < MinValColorChannel)
                            {
                                MinValColorChannel = pixel.R;
                            }
                        }
                    }
                } else if (colorChannel == 'G') { // color channel is green
                    MinValColorChannel = _.GetRowSpan(root.y)[root.x].G;
                    for (int y = root.y; y < root.y + root.height; y++)
                    {
                        var row = _.GetRowSpan(y);
                        for (int x = root.x; x < root.x + root.width; x++)
                        {
                            var pixel = row[x];
                            if (pixel.G < MinValColorChannel)
                            {
                                MinValColorChannel = pixel.G;
                            }
                        }
                    }
                } else if (colorChannel == 'B') { // if blue
                    MinValColorChannel = _.GetRowSpan(root.y)[root.x].B;
                    for (int y = root.y; y < root.y + root.height; y++)
                    {
                        var row = _.GetRowSpan(y);
                        for (int x = root.x; x < root.x + root.width; x++)
                        {
                            var pixel = row[x];
                            if (pixel.B < MinValColorChannel)
                            {
                                MinValColorChannel = pixel.B;
                            }
                        }
                    }
                }
            });
        }

        return MinValColorChannel;
    }

    // count the error boundary of Image tree : Entropy
    public static double EntropyImageTree(ImageTree root){
        // get each color channel entropy value
        double redEntropy = EntropyImageTreeColorChannel(root, 'R');
        double blueEntropy = EntropyImageTreeColorChannel(root, 'B');
        double greenEntropy = EntropyImageTreeColorChannel(root, 'G');

        // debug
        // Console.WriteLine($"Entropy imageTree : {(blueEntropy + redEntropy + greenEntropy) / 3}");

        return (blueEntropy + redEntropy + greenEntropy) / 3;
    }

    // count the entropy of one color channel 
    public static double EntropyImageTreeColorChannel (ImageTree root, char colorChannel) {
        double totalPixel = root.width * root.height;
        double entropy = 0;
        double[] probColorChannel = new double[256];
        
        // fill the probColorChannel
        if (root.image != null)
        {
            root.image.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        var pixel = row[x];
                        int value = colorChannel switch
                        {
                            'R' => pixel.R,
                            'G' => pixel.G,
                            'B' => pixel.B,
                            _ => throw new ArgumentException("Invalid color channel")
                        };

                        probColorChannel[value]++;
                    }
                }
            });
        }

        // divide all the probColorChannel with the total pixel to get the probability and sum it all up
        for (int i = 0; i < probColorChannel.Length; i++)
        {
            probColorChannel[i] /= totalPixel;
            if (probColorChannel[i] != 0)
            {
                entropy -= probColorChannel[i] * Math.Log2(probColorChannel[i]);
            }
        }                

        return entropy;
    }

    // bonus : ssim method

    // normalize image tree if it cannot be divided anymore
    public static void NormalizeImageTree(ImageTree root){
        // assumption : the imageTree cannot be divided anymore

        // debug
        // printImageTree(root);

        // count each rgb value of the Image Tree and find the mean
        double RedMean = MeanChannelColorValue(root, 'R');
        double BlueMean = MeanChannelColorValue(root, 'B');
        double GreenMean = MeanChannelColorValue(root, 'G');

        // debug
        // Console.WriteLine($"Mean pixel value : ({RedMean}, {GreenMean}, {BlueMean})");

        // set every rgb of the Image Tree based on the mean value
        if (root.image != null && root.child != null)
        {
            root.image.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        var pixel = row[x];

                        // set new value
                        row[x] = new Rgba32(
                            (byte) RedMean,
                            (byte) GreenMean,
                            (byte) BlueMean,
                            pixel.A
                        );
                    }
                }
            });
            root.child.LeftBottom = null;
            root.child.RightBottom = null;
            root.child.LeftTop = null;
            root.child.RightTop = null;
        }
    }

    // print image tree status
    public static void printImageTree(ImageTree root) {
        if (root != null)
        {
            // read all the value of ImageTree
            Console.WriteLine($"x : {root.x}");
            Console.WriteLine($"y : {root.y}");
            Console.WriteLine($"width : {root.width}");
            Console.WriteLine($"height : {root.height}");
            Console.WriteLine($"rgba : {root.rgba}");
            Console.WriteLine($"parent : {root.parent}");
            Console.WriteLine($"child : {root.child}");

            // do next print info of image tree
            if (root.child != null)
            {
                if (root.child.LeftBottom != null)
                {
                    printImageTree(root.child.LeftBottom); 
                }
                if (root.child.RightBottom != null)
                {
                    printImageTree(root.child.RightBottom); 
                }
                if (root.child.LeftTop != null)
                {
                    printImageTree(root.child.LeftTop); 
                }
                if (root.child.RightTop != null)
                {
                    printImageTree(root.child.RightTop); 
                }
            }
        } else {
            // do nothing; you may insert your error handling here
        }
    }
}

class Program {
    static void Main(string[] args) {

    }
}