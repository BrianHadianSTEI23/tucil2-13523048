using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ChildTree {
    public ImageTree? LeftTop;  // may be null  
    public ImageTree? RightTop;    
    public ImageTree? LeftBottom;    
    public ImageTree? RightBottom;    
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
    
    // operations
    // build the recursive tree
    public static void BuildTree(ImageTree root, int minWidth, int minHeight, double threshold, int? errorDetectionMethod, double compressionTarget) {
        // check if it's still possible to be partitioned
        if ((root.width / 4) >= minWidth && (root.height / 4) >= minHeight)
        {
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
                        /*
                        if the image has odd size of pixels, then the guideline is to left and top frame will always have the fewer ones when pixels is divided into two, but to right and bottom will have the remaining ones
                        */
                        // left top
                        ImageTree LeftTop = new ImageTree(root.image, root.x, root.y, root.width / 2, root.height / 2, root.rgba, root, null);
                        BuildTree(LeftTop, minWidth, minHeight, threshold, errorDetectionMethod, compressionTarget);

                        // right top
                        Rgba32 rgbaRightTop = default;
                        root.image.ProcessPixelRows(_ => { // get the rgba of the current (x, y)
                            rgbaRightTop = _.GetRowSpan(root.y)[root.x + (root.width / 2)];
                        });
                        ImageTree RightTop = new ImageTree(root.image, root.x + (root.width / 2), root.y, root.width % 2 == 0 ? root.width / 2 : (root.width / 2) + 1, root.height / 2, rgbaRightTop, root, null);
                        BuildTree(RightTop, minWidth, minHeight, threshold, errorDetectionMethod, compressionTarget);

                        // Left bottom
                        Rgba32 rgbaLeftBottom = default;
                        root.image.ProcessPixelRows(_ => { // get the rgba of the current (x, y)
                            rgbaLeftBottom = _.GetRowSpan(root.y + (root.height / 2))[root.x];
                        });
                        ImageTree LeftBottom = new ImageTree(root.image, root.x, root.y + (root.height / 2), root.width / 2, root.height % 2 == 0 ? root.height / 2 : (root.height / 2) + 1, rgbaLeftBottom, root, null);
                        BuildTree(LeftBottom, minWidth, minHeight, threshold, errorDetectionMethod, compressionTarget);

                        // Right Bottom
                        Rgba32 rgbaRightBottom = default;
                        root.image.ProcessPixelRows(_ => { // get the rgba of the current (x, y)
                            rgbaRightBottom = _.GetRowSpan(root.y + (root.height / 2))[root.x + (root.width / 2)];
                        });
                        ImageTree RightBottom = new ImageTree(root.image, root.x + (root.width / 2), root.y + (root.height / 2), root.width / 2, root.height % 2 == 0 ? root.height / 2 : (root.height / 2) + 1, rgbaRightBottom, root, null);
                        BuildTree(RightBottom, minWidth, minHeight, threshold, errorDetectionMethod, compressionTarget);
                    }
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
    public static void BuildImageFromImageTree(ImageTree root, ref Image<Rgba32> constructImage){

        // fill the constructImage
        if (root != null)
        {
            constructImage.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
                    var row = _.GetRowSpan(y);
                    for (int x = root.x; x < root.x + root.width; x++)
                    {
                        // set new value
                        row[x] = root.rgba;
                    }
                }
            });
        }

        return ;
    }

    // count variance of four of the child : variance
    public static double ImageTreeVariance(ImageTree root) {
        // assumption : root is always safe to be partitioned
        int n = root.width * root.height;

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

    public static double MeanChannelColorValue(ImageTree root, char colorChannel) {
        int totalValueColorChannel = 0;

        if (root.image != null)
        {
            root.image.ProcessPixelRows(_ => {
                for (int y = root.y; y < root.y + root.height; y++)
                {
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
        // Console.WriteLine();

        return entropy;
    }

    // normalize image tree if it cannot be divided anymore
    public static void NormalizeImageTree(ImageTree root){
        // assumption : the imageTree cannot be divided anymore

        // count each rgb value of the Image Tree and find the mean
        double RedMean = MeanChannelColorValue(root, 'R');
        double BlueMean = MeanChannelColorValue(root, 'B');
        double GreenMean = MeanChannelColorValue(root, 'G');

        // debug
        // Console.WriteLine($"Mean pixel value : ({RedMean}, {GreenMean}, {BlueMean})");

        // set every rgb of the Image Tree based on the mean value
        if (root.image != null)
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