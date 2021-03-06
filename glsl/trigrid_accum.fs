#version 140
#extension GL_ARB_shading_language_include : require
#extension GL_ARB_gpu_shader5 : enable
uniform uint stage;
// used by accumulation pass
uniform sampler2DArray kinect_colors;
uniform sampler2DArray kinect_normals;
uniform sampler2DArray depth_map_curr;
uniform uint layer;

uniform mat4 img_to_eye_curr;
uniform vec2 viewportSizeInv;
uniform float epsilon;

uniform mat4 gl_NormalMatrix;

uniform vec3[5] camera_positions;

#include </shading.glsl>

#include </inc_bbox_test.glsl>
///////////////////////////////////////////////////////////////////////////////
// input
///////////////////////////////////////////////////////////////////////////////
in vec2  pass_texcoord;
in vec3  pass_pos_es;
in vec3  pass_pos_cs;

in float pass_depth;
in float pass_quality;
in vec3  pass_normal_es;

out vec4 gl_FragColor;
// methods 

///////////////////////////////////////////////////////////////////////////////
// main
///////////////////////////////////////////////////////////////////////////////
void main() {

  if(!in_bbox(pass_pos_cs)) {
    discard;
  }

  // to cull away borders of the rgb camera view
  if(pass_texcoord.s > 0.99 || pass_texcoord.s < 0.01 ||
    pass_texcoord.t > 0.99 || pass_texcoord.t < 0.01) {
      discard;
  }

  vec3 normal = -normalize(pass_normal_es);
  // backface culling
  if ( dot ( normal, normalize(pass_pos_es) ) > 0.0 ) {
   discard;
  }
  float quality = pass_quality;

  if(stage > 0u){ // accumulation pass write color and quality if within epsilon
    vec3  coords = vec3(gl_FragCoord.xy * viewportSizeInv, 0.0 /*here layer is always 0*/);
    float depth_curr = texture(depth_map_curr, coords).r;
    vec4  position_curr = img_to_eye_curr * vec4(gl_FragCoord.xy + vec2(0.5,0.5),depth_curr,1.0);
    vec3  position_curr_es = position_curr.xyz / position_curr.w;
    // discard if occluded by triangles in front
    if(epsilon < length(position_curr_es - pass_pos_es)) {
      discard;
      return;
    }

    vec3 color = texture(kinect_colors, vec3(pass_texcoord, float(layer))).rgb;
    if (g_shade_mode == 3u) {
      gl_FragColor = vec4(camera_colors[layer] * quality, quality);
    }
    else {
      gl_FragColor = vec4(shade(pass_pos_es, normal, color) * quality, quality);
    }
   }
}