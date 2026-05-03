shader_type canvas_item;
render_mode blend_mix;

// =============================================
// 黑洞核心着色器 - Blackhole Core Shader
// =============================================

// === 可配置参数 ===
uniform float blackhole_size : hint_range(10.0, 500.0) = 100.0;
uniform float pulse_frequency : hint_range(0.1, 5.0) = 1.5;
uniform float pulse_intensity : hint_range(0.0, 0.5) = 0.15;
uniform float swirl_strength : hint_range(0.0, 2.0) = 0.8;
uniform float time_offset = 0.0;

// === 常量 ===
const vec3 EDGE_GLOW_COLOR = vec3(0.29, 0.05, 0.18); // 暗紫红色 #4a0e2e
const float EDGE_GLOW_BASE = 0.6;
const float SINGULARITY_TWINKLE_FREQ = 3.0;
const float SINGULARITY_TWINKLE_INTENSITY = 0.3;

// === 工具函数 ===

// 计算到中心的距离（考虑纹理尺寸）
float dist_to_center(vec2 uv, vec2 texture_size) {
    vec2 center = vec2(0.5, 0.5);
    vec2 delta = uv - center;
    // 考虑宽高比
    delta.x *= texture_size.x / texture_size.y;
    return length(delta);
}

// 漩涡扭曲UV
vec2 swirl_uv(vec2 uv, float strength, float time) {
    vec2 center = vec2(0.5, 0.5);
    vec2 delta = uv - center;
    float dist = length(delta);
    float angle = atan(delta.y, delta.x);
    
    // 漩涡角度随距离增加
    float swirl_angle = strength * (1.0 - dist * 2.0) * 6.28318;
    swirl_angle += time * 2.0; // 持续旋转
    
    angle += swirl_angle * smoothstep(1.0, 0.0, dist * 2.0);
    
    vec2 new_delta = vec2(cos(angle), sin(angle)) * dist;
    return center + new_delta;
}

// 奇点闪烁
float singularity_twinkle(float time) {
    float t = time * SINGULARITY_TWINKLE_FREQ;
    float noise = sin(t) * 0.5 + sin(t * 1.7) * 0.3 + sin(t * 2.3) * 0.2;
    return 1.0 + noise * SINGULARITY_TWINKLE_INTENSITY;
}

// === 主函数 ===
void fragment() {
    float time = TIME + time_offset;
    vec2 texture_size = vec2(textureSize(TEXTURE, 0));
    
    // 1. 计算基础距离
    float dist = dist_to_center(UV, texture_size);
    
    // 2. 律动效果 - 周期性大小变化
    float pulse = sin(time * pulse_frequency * 6.28318) * 0.5 + 0.5;
    float current_size = blackhole_size * (1.0 + pulse * pulse_intensity);
    
    // 将距离转换为基于黑洞大小的归一化距离
    float normalized_dist = dist * min(texture_size.x, texture_size.y) / current_size;
    
    // 3. 应用漩涡扭曲（仅在边缘区域）
    vec2 swirled_uv = swirl_uv(UV, swirl_strength * smoothstep(0.3, 1.0, normalized_dist), time);
    
    // 使用扭曲后的UV采样纹理（用于alpha遮罩）
    float tex_alpha = texture(TEXTURE, swirled_uv).a;
    
    // 4. 黑洞主体 - 纯黑色，边缘虚化
    float edge_softness = 0.15 + pulse * 0.05; // 律动影响边缘柔和度
    float alpha = 1.0 - smoothstep(0.0, edge_softness, normalized_dist - 0.85);
    alpha *= tex_alpha;
    
    // 5. 边缘辉光（Event Horizon）
    float glow_width = 0.08;
    float glow_start = 0.85 - glow_width;
    float glow_mask = smoothstep(glow_start, glow_start + glow_width * 0.3, normalized_dist) 
                    * smoothstep(1.0, 0.9, normalized_dist);
    
    // 辉光强度律动
    float glow_intensity = EDGE_GLOW_BASE + sin(time * pulse_frequency * 2.0) * 0.2;
    glow_intensity *= glow_mask;
    
    vec3 glow = EDGE_GLOW_COLOR * glow_intensity;
    
    // 6. 奇点闪烁（中心极暗区域）
    float singularity_mask = 1.0 - smoothstep(0.0, 0.3, normalized_dist);
    float twinkle = singularity_twinkle(time);
    vec3 singularity_color = vec3(0.02, 0.0, 0.01) * twinkle * singularity_mask;
    
    // 7. 合成颜色
    vec3 final_color = vec3(0.0); // 基础黑色
    final_color += glow; // 边缘辉光
    final_color += singularity_color; // 奇点闪烁
    
    // 8. 最终输出
    COLOR = vec4(final_color, alpha);
}
