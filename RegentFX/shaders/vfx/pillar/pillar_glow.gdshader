shader_type canvas_item;

// =============================================
// Pillar 外发光 Shader
// 基于 Alpha 通道的径向外发光，可扩散出 Sprite 边界
// =============================================

// 外发光强度
uniform float glow_intensity : hint_range(0.0, 5.0) = 1.5;
// 外发光半径（像素）
uniform float glow_radius : hint_range(0.0, 64.0) = 24.0;
// 外发光颜色
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
// 内发光强度（实体边缘的发光）
uniform float inner_glow : hint_range(0.0, 1.0) = 0.2;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    vec2 ps = TEXTURE_PIXEL_SIZE;
    
    float glow_alpha = 0.0;
    
    // 16方向 x 3层径向采样
    for (int i = 0; i < 16; i++) {
        float angle = float(i) * 0.3926990816987; // 2PI / 16
        vec2 dir = vec2(cos(angle), sin(angle));
        
        // 三层距离采样，权重递减
        glow_alpha += texture(TEXTURE, UV + dir * glow_radius * 0.33 * ps).a * 0.5;
        glow_alpha += texture(TEXTURE, UV + dir * glow_radius * 0.66 * ps).a * 0.3;
        glow_alpha += texture(TEXTURE, UV + dir * glow_radius * 1.00 * ps).a * 0.2;
    }
    
    // 平均
    glow_alpha /= 16.0;
    
    // 外发光：只在原图透明区域显示，避免遮盖原图
    float outer_glow = glow_alpha * (1.0 - tex_color.a) * glow_intensity;
    
    // 内发光：实体像素边缘也有微弱发光
    float edge_glow = tex_color.a * inner_glow * glow_intensity;
    
    // 合成颜色
    vec3 glow_rgb = glow_color.rgb * (outer_glow + edge_glow);
    vec3 final_rgb = tex_color.rgb + glow_rgb;
    
    // Alpha：保留原图alpha，并加上外发光alpha
    float final_alpha = max(tex_color.a, outer_glow * glow_color.a);
    
    COLOR = vec4(final_rgb, final_alpha);
}
