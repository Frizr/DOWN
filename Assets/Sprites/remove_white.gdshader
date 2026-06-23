shader_type canvas_item;

uniform vec4 key_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float tolerance : hint_range(0.0, 1.0) = 0.25;
uniform float flash_amount : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    // Calculate distance between texture color and key color
    float diff = distance(tex_color.rgb, key_color.rgb);
    
    // If it's close to the key color (white), discard it (make transparent)
    if (diff < tolerance) {
        COLOR = vec4(tex_color.rgb, 0.0);
    } else {
        vec3 final_color = mix(tex_color.rgb, vec3(1.0, 1.0, 1.0), flash_amount);
        COLOR = vec4(final_color, tex_color.a);
    }
}
