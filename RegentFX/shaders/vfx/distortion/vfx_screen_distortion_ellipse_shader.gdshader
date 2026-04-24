shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

uniform float distortion_base_intensity = 0.05;
uniform float aspect_ratio = 1.0;

varying vec4 vertex_color;
varying float lifetime;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
}

void fragment()
{
	// 将 UV 转换为椭圆坐标系 (aspect_ratio > 1 时Y轴压缩，< 1 时Y轴拉伸)
	vec2 elliptical_uv = UV;
	elliptical_uv.y = (elliptical_uv.y - 0.5) / aspect_ratio + 0.5;
	
	vec2 direction = normalize(vec2(0.5, 0.5) - elliptical_uv);
	// 调整方向向量以匹配椭圆形状
	direction.y = direction.y / aspect_ratio;
	direction = normalize(direction);

	float distortion_mask = texture(TEXTURE, UV).r;
	float distortion_intensity = vertex_color.a;
	
	vec4 screen_tex = textureLod(
		screen_texture,
		SCREEN_UV + (distortion_base_intensity * distortion_intensity * distortion_mask * direction),
		0.0
		);
		
	COLOR = screen_tex;
}
