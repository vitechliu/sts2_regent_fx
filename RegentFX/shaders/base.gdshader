shader_type canvas_item;

// 发光强度
uniform float glow_intensity : hint_range(0.0, 2.0) = 0.5;
// 发光颜色
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 0.8, 1.0);
// 模糊强度 (0 = 无模糊, 大于0产生模糊)
uniform float blur_intensity : hint_range(0.0, 10.0) = 0.0;
// 模糊质量 (采样次数，数值越高性能越差，但效果更好)
uniform int blur_samples : hint_range(4, 32) = 8;

// 获取模糊后的像素颜色
vec4 blur_texture(sampler2D tex, vec2 uv, float intensity, int samples) {
    if (intensity <= 0.0) {
        return texture(tex, uv);
    }
    
    vec4 color = vec4(0.0);
    float total_weight = 0.0;
    
    // 采样半径随强度增加
    float radius = intensity * 0.02;
    
    for (int i = -samples/2; i <= samples/2; i++) {
        for (int j = -samples/2; j <= samples/2; j++) {
            // 修正：将 int 转换为 float
            vec2 offset = vec2(float(i), float(j)) * radius;
            vec2 sample_uv = uv + offset;
            
            // 边界处理（边缘不采样超出部分）
            if (sample_uv.x >= 0.0 && sample_uv.x <= 1.0 && 
                sample_uv.y >= 0.0 && sample_uv.y <= 1.0) {
                vec4 sample_color = texture(tex, sample_uv);
                float weight = 1.0 / (1.0 + length(vec2(float(i), float(j))));
                color += sample_color * weight;
                total_weight += weight;
            }
        }
    }
    
    return color / total_weight;
}

void fragment() {
    // 获取原始纹理颜色
    vec4 tex_color = texture(TEXTURE, UV);
    
    // 1. 应用模糊效果
    vec4 blurred_color = blur_texture(TEXTURE, UV, blur_intensity, blur_samples);
    
    // 2. 计算发光效果
    // 提取亮度（用于控制发光区域，只让较亮的像素发光）
    float brightness = (tex_color.r + tex_color.g + tex_color.b) / 3.0;
    // 或使用标准亮度公式
    // float brightness = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    
    // 亮度阈值：只有亮度超过0.3的区域才产生明显发光
    float glow_factor = smoothstep(0.3, 0.8, brightness) * glow_intensity;
    
    // 发光颜色叠加
    vec3 glow_rgb = glow_color.rgb * glow_factor;
    
    // 3. 合成最终颜色
    vec3 final_rgb = blurred_color.rgb + glow_rgb;
    
    float final_alpha = tex_color.a * COLOR.a;
	COLOR = vec4(final_rgb, final_alpha);
}