#version 330 core
//#include dither.fsh

precision highp float;

in vec2 uv;
out vec4 outColor;

uniform sampler2D iColor;

uniform float iVigneteStrength;

vec4 Color = texture(iColor, uv);

vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main () 
{
    //vec3 hsv = rgb2hsv(Color.xyz);
    //vec3 rgb = hsv2rgb(vec3(hsv.x, hsv.y * 0.6, (hsv.z * 0.75) + 0.25));
	//outColor = vec4(rgb.xyz, 1.0);
    float dist = distance(vec2(0.5,0.5), uv.xy);
    dist = dist - (1.5 - (1.5 * iVigneteStrength));
    dist = 1 - max(dist, 0.0);
    outColor = vec4(Color.xyz * dist * dist * dist * dist, 1.0);
}
