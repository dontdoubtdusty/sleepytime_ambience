#version 330

// Lo-fi post-process for the 640x360 scene picture: soft bloom, a little colour fringing,
// warm grading, a limited colour palette with ordered dithering, light film grain and faint scanlines.
// Everything is kept gentle: this is watched half-asleep in a dark room.

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec2 texSize;   // size of the scene picture in pixels
uniform float time;     // seconds, drives the grain

out vec4 finalColor;

// Colour steps per channel (lower = chunkier palette).
const float LEVELS = 14.0;
const float BLOOM = 0.55;
const float GRAIN = 0.035;
const float SCANLINE = 0.05;

float luma(vec3 c) { return dot(c, vec3(0.299, 0.587, 0.114)); }

// 4x4 ordered-dither threshold in the range -0.5 .. 0.5 for a given scene pixel.
float bayer(vec2 p)
{
    int x = int(mod(p.x, 4.0));
    int y = int(mod(p.y, 4.0));
    int i = y * 4 + x;
    float m[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
    return m[i] / 16.0 - 0.5;
}

float hash(vec2 p)
{
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

// Only the bright parts of a sample feed the bloom.
vec3 bright(vec2 uv)
{
    vec3 c = texture(texture0, uv).rgb;
    float b = smoothstep(0.45, 0.95, luma(c));
    return c * b;
}

void main()
{
    vec2 uv = fragTexCoord;
    vec2 px = 1.0 / texSize;

    // Slight colour fringing, stronger toward the edges.
    vec2 off = (uv - 0.5) * 0.0035;
    vec3 col;
    col.r = texture(texture0, uv + off).r;
    col.g = texture(texture0, uv).g;
    col.b = texture(texture0, uv - off).b;

    // Cheap bloom: two rings of taps around the pixel.
    vec3 glow = vec3(0.0);
    for (int i = 0; i < 8; i++)
    {
        float a = float(i) * 0.785398;
        vec2 d = vec2(cos(a), sin(a));
        glow += bright(uv + d * px * 3.0);
        glow += bright(uv + d * px * 7.0) * 0.7;
    }
    col += glow / 8.0 * BLOOM;

    // Warm grading: tint, soft lift of the blacks, gentle contrast.
    col *= vec3(1.06, 0.98, 0.88);
    col = col * 0.96 + vec3(0.018, 0.012, 0.024);
    col = clamp(col, 0.0, 1.0); // the contrast curve below goes wrong above 1
    col = mix(col, col * col * (3.0 - 2.0 * col), 0.25);

    // Limited palette with ordered dithering on the scene's own pixel grid.
    vec2 scenePx = floor(uv * texSize);
    col = floor(col * LEVELS + bayer(scenePx) * 0.9 + 0.5) / LEVELS;

    // Film grain that changes about 12 times a second, and faint scanlines.
    float g = hash(scenePx + floor(time * 12.0) * 17.0) - 0.5;
    col += g * GRAIN;
    col *= 1.0 - SCANLINE * mod(floor(gl_FragCoord.y), 2.0);

    finalColor = vec4(clamp(col, 0.0, 1.0), 1.0) * colDiffuse * fragColor;
}
