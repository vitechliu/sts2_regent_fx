shader_type canvas_item;
render_mode blend_mix;

uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float tint_intensity = 1.0;
uniform int tint_mode = 0; // 0=Multiply, 1=Screen, 2=Overlay, 3=HueShift

vec3 rgb_to_hsv(vec3 c) {
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
	vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv_to_rgb(vec3 c) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment() {
	vec4 tex_color = texture(TEXTURE, UV);
	vec3 base = tex_color.rgb;

	if (tint_mode == 0) {
		// Multiply
		base = mix(base, base * tint_color.rgb, tint_intensity);
	} else if (tint_mode == 1) {
		// Screen: 更亮
		base = mix(base, vec3(1.0) - (vec3(1.0) - base) * (vec3(1.0) - tint_color.rgb), tint_intensity);
	} else if (tint_mode == 2) {
		// Overlay
		vec3 blended = mix(
			2.0 * base * tint_color.rgb,
			vec3(1.0) - 2.0 * (vec3(1.0) - base) * (vec3(1.0) - tint_color.rgb),
			step(0.5, base)
		);
		base = mix(base, blended, tint_intensity);
	} else if (tint_mode == 3) {
		// HueShift: 保持亮度，只改变色相
		vec3 hsv_tex = rgb_to_hsv(base);
		vec3 hsv_tint = rgb_to_hsv(tint_color.rgb);
		hsv_tex.x = mix(hsv_tex.x, hsv_tint.x, tint_intensity);
		hsv_tex.y = mix(hsv_tex.y, hsv_tint.y, tint_intensity);
		base = hsv_to_rgb(hsv_tex);
	}

	COLOR = vec4(base, tex_color.a) * COLOR;
}
