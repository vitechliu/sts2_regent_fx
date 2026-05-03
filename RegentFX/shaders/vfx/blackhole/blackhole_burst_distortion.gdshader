shader_type canvas_item;
render_mode blend_mix;

// =============================================
// 黑洞爆发屏幕扭曲着色器 - Blackhole Burst Distortion
// =============================================

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

// === 可配置参数 ===
uniform float distortion_intensity : hint_range(0.0, 0.3) = 0.0;
uniform float distortion_radius : hint_range(100.0, 1000.0) = 600.0;
uniform vec2 center_uv = vec2(0.5, 0.5);
uniform float wave_count : hint_range(1.0, 8.0) = 4.0;
uniform float time_offset = 0.0;

// === 常量 ===
const float DECAY_SPEED = 0.5;
const float WAVE_SPEED = 3.0;

// === 工具函数 ===

// 计算到中心的距离
float dist_to_center(vec2 uv, vec2 center) {
    vec2 delta = uv - center;
    return length(delta);
}

// 多层波纹扭曲
vec2 burst_distortion(vec2 uv, vec2 center, float intensity, float radius, float time) {
    vec2 delta = uv - center;
    float dist = length(delta);
    
    // 归一化距离（基于影响半径）
    float normalized_dist = dist / (radius / 1000.0);
    
    // 影响范围遮罩（只在半径内生效，边缘平滑过渡）
    float influence = smoothstep(1.0, 0.3, normalized_dist);
    
    // 多层波纹
    vec2 distortion = vec2(0.0);
    for (float i = 1.0; i <= 4.0; i++) {
        float wave_freq = i * 8.0;
        float wave_phase = time * WAVE_SPEED * (1.0 + i * 0.3);
        float wave_amp = intensity / i; // 高频波纹幅度递减
        
        // 向外扩散的波纹
        float wave = sin(normalized_dist * wave_freq - wave_phase);
        
        // 波纹随距离衰减
        float wave_decay = exp(-normalized_dist * 2.0);
        
        // 方向扭曲（沿径向）
        vec2 dir = normalize(delta + vec2(0.0001)); // 避免除零
        distortion += dir * wave * wave_amp * wave_decay * influence;
    }
    
    // 额外添加漩涡扭曲
    float swirl = sin(time * 2.0 + normalized_dist * 10.0) * intensity * 0.3;
    vec2 swirl_dir = vec2(-delta.y, delta.x);
    swirl_dir = normalize(swirl_dir + vec2(0.0001));
    distortion += swirl_dir * swirl * influence * smoothstep(0.0, 0.5, normalized_dist);
    
    return distortion;
}

// === 主函数 ===
void fragment() {
    float time = TIME + time_offset;
    
    // 1. 计算扭曲偏移
    vec2 distortion = burst_distortion(UV, center_uv, distortion_intensity, distortion_radius, time);
    
    // 2. 应用扭曲采样屏幕
    vec2 distorted_uv = SCREEN_UV + distortion;
    vec4 screen_color = textureLod(screen_texture, distorted_uv, 0.0);
    
    // 3. 计算影响遮罩（用于alpha混合）
    float dist = dist_to_center(UV, center_uv);
    float normalized_dist = dist / (distortion_radius / 1000.0);
    float alpha = smoothstep(1.0, 0.5, normalized_dist) * smoothstep(0.0, 0.1, distortion_intensity);
    
    // 4. 输出（只显示扭曲效果，不显示本体颜色）
    COLOR = vec4(screen_color.rgb, alpha);
}
