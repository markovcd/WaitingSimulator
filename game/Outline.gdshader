// The ink line round a shape: its back faces, pushed out and drawn flat, show as a rim past the front ones.
shader_type spatial;
render_mode unshaded, cull_front, shadows_disabled;

uniform vec3 color : source_color = vec3(0.08, 0.07, 0.07);

// The line's width per meter away from the eyes, so it stays about as thick on screen near and far, within its limits in meters.
uniform float width_per_meter = 0.004;
uniform float min_width = 0.003;
uniform float max_width = 0.03;

// A box's corners have a normal per face, which would split the rim at every edge; pushing each corner out along
// every axis keeps it whole.
uniform bool square = false;

void vertex() {
    float away = -(MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).z;
    VERTEX += (square ? sign(VERTEX) : NORMAL) * clamp(away * width_per_meter, min_width, max_width);
}

void fragment() {
    ALBEDO = color;
}
