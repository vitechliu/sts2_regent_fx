shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float distortion_strength : hint_range(0.0, 0.04) = 0.018;
uniform float noise_strength : hint_range(0.0, 0.03) = 0.011;
uniform float noise_scale : hint_range(1.0, 20.0) = 8.0;
uniform float noise_speed : hint_range(0.0, 20.0) = 10.0;
uniform float swirl_strength : hint_range(-0.03, 0.03) = 0.008;
uniform float chromatic_aberration : hint_range(0.0, 0.01) = 0.0035;
uniform float aspect_ratio : hint_range(0.1, 2.0) = 0.75;
uniform float noise_seed = 0.0;

varying vec4 particle_color;
varying float particle_lifetime;

float random_2d(vec2 point) {
	return fract(sin(dot(point, vec2(127.1, 311.7))) * 43758.5453123);
}

float value_noise(vec2 point) {
	vec2 cell = floor(point);
	vec2 fraction = fract(point);
	vec2 smooth_fraction = fraction * fraction * (3.0 - 2.0 * fraction);

	float bottom_left = random_2d(cell);
	float bottom_right = random_2d(cell + vec2(1.0, 0.0));
	float top_left = random_2d(cell + vec2(0.0, 1.0));
	float top_right = random_2d(cell + vec2(1.0, 1.0));

	return mix(
		mix(bottom_left, bottom_right, smooth_fraction.x),
		mix(top_left, top_right, smooth_fraction.x),
		smooth_fraction.y);
}

void vertex() {
	particle_color = COLOR;
	particle_lifetime = INSTANCE_CUSTOM.y / max(INSTANCE_CUSTOM.w, 0.0001);
}

void fragment() {
	float ring_mask = texture(TEXTURE, UV).r * particle_color.a;
	vec2 local = UV - vec2(0.5);
	local.x *= aspect_ratio;

	float local_length = max(length(local), 0.0001);
	vec2 radial = local / local_length;
	vec2 tangent = vec2(-radial.y, radial.x);
	float life_pulse = sin(clamp(particle_lifetime, 0.0, 1.0) * 3.14159265);

	vec2 animated_noise_uv = local * noise_scale;
	animated_noise_uv += vec2(
		TIME * noise_speed + noise_seed,
		-TIME * noise_speed * 0.73 + noise_seed * 1.37);
	float noise_x = value_noise(animated_noise_uv);
	float noise_y = value_noise(animated_noise_uv.yx + vec2(19.17, -7.31));
	vec2 noise_vector = vec2(noise_x, noise_y) * 2.0 - 1.0;

	float ripple = sin(local_length * 52.0 - TIME * 25.0 + noise_seed) * 0.35 + 0.65;
	vec2 warp = radial * distortion_strength * ripple;
	warp += tangent * swirl_strength * (noise_x * 2.0 - 1.0);
	warp += noise_vector * noise_strength;
	warp *= ring_mask * life_pulse;

	vec2 chroma_direction = radial * chromatic_aberration * ring_mask * life_pulse;
	vec2 red_uv = clamp(SCREEN_UV + warp + chroma_direction, vec2(0.001), vec2(0.999));
	vec2 green_uv = clamp(SCREEN_UV + warp, vec2(0.001), vec2(0.999));
	vec2 blue_uv = clamp(SCREEN_UV + warp - chroma_direction, vec2(0.001), vec2(0.999));

	COLOR = vec4(
		textureLod(screen_texture, red_uv, 0.0).r,
		textureLod(screen_texture, green_uv, 0.0).g,
		textureLod(screen_texture, blue_uv, 0.0).b,
		ring_mask);
}
