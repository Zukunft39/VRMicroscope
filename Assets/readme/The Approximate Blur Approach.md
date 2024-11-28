# The Approximate Blur Approach

- Context
    Due to limitation of multiple passes in shader graph, the traditional Gaussian blur approach, which requires two shader passes to represent the two dimensions of the convolution kernel, is difficult to implement in our context. We need to use an alternative approach.
- Principle
    According to the principle of box lowpass filter and Gaussian low-pass filter, we need to average the center pixel with the neighboring pixels in different directions. For each pixel, if we offset the original texture in eight directions and then sum them up with original texture, we will achieve an approximate effect of traditional low-pass filters.
