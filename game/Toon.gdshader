// The look every surface shares: flat facets lit in three hard bands, so primitives read as low-poly and cel shaded.
shader_type spatial;

uniform vec3 albedo : source_color = vec3(1.0);

void fragment() {
    ALBEDO = albedo;
    // The facet's own normal, turned to face the way the smooth one does, whichever way the screen's y runs.
    vec3 facet = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
    NORMAL = dot(facet, NORMAL) < 0.0 ? -facet : facet;
}

void light() {
    float lit = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
    // Unlit, half lit or fully lit, with edges a hair soft so they do not shimmer.
    float band = 0.6 * smoothstep(0.05, 0.1, lit) + 0.4 * smoothstep(0.55, 0.6, lit);
    DIFFUSE_LIGHT += band * LIGHT_COLOR / PI;
}
