shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform bool show_wireframe = false;
uniform vec4 wire_color : source_color = vec4(0.92, 0.95, 0.98, 0.38);

void fragment() {
	vec3 base_color = COLOR.rgb;
	if (show_wireframe) {
		float third = max(0.0, 1.0 - UV.x - UV.y);
		float distance_to_edge = min(UV.x, min(UV.y, third));
		float derivative = max(fwidth(distance_to_edge), 0.0001);
		float edge = 1.0 - smoothstep(derivative * 0.7, derivative * 1.8, distance_to_edge);
		base_color = mix(base_color, wire_color.rgb, edge * wire_color.a);
	}

	ALBEDO = base_color;
	ROUGHNESS = 0.88;
	METALLIC = 0.0;
}
