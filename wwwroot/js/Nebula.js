const canvas = document.getElementById('nebula');
const gl = canvas.getContext('webgl2');
if (!gl) {
    alert("WebGL2 not supported on this device.");
}

// --- Resolution scaling (good for mobile performance) ---
const scale = window.devicePixelRatio > 1 ? 0.5 : 1.0;
canvas.width = Math.floor(window.innerWidth * scale);
canvas.height = Math.floor(window.innerHeight * scale);
gl.viewport(0, 0, canvas.width, canvas.height);

// Vertex shader: draws fullscreen quad
const vsSource = `#version 300 es
            precision mediump float;
            const vec2 pos[6] = vec2[6](
                vec2(-1.0,-1.0), vec2(1.0,-1.0), vec2(-1.0,1.0),
                vec2(-1.0,1.0), vec2(1.0,-1.0), vec2(1.0,1.0)
            );
            out vec2 uv;
            void main() {
                uv = pos[gl_VertexID] * 0.5 + 0.5;
                gl_Position = vec4(pos[gl_VertexID], 0.0, 1.0);
            }`;

// Fragment shader with tunable parameters
const fsSource = `#version 300 es
            precision highp float;
            out vec4 fragColor;
            in vec2 uv;

            uniform float time;
            uniform vec2 resolution;

            // Tunable parameters
            uniform int octaves;          // number of fbm layers
            uniform float pScale;         // base scale multiplier
            uniform float qScale;         // turbulence strength
            uniform float timeScale;      // motion speed
            uniform vec3 color1;          // base color
            uniform vec3 color2;          // highlight color

            float rand(vec2 p) {
                return fract(sin(dot(p, vec2(12.9898,78.233))) * 43758.5453);
            }

            float noise(vec2 p) {
                vec2 i = floor(p);
                vec2 f = fract(p);
                float a = rand(i);
                float b = rand(i + vec2(1.0, 0.0));
                float c = rand(i + vec2(0.0, 1.0));
                float d = rand(i + vec2(1.0, 1.0));
                vec2 u = f*f*(3.0 - 2.0*f);
                return mix(a,b,u.x) + (c-a)*u.y*(1.0-u.x) + (d-b)*u.x*u.y;
            }

            float fbm(vec2 p) {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 10; i++) {   // upper bound loop
                    if (i >= octaves) break;     // stop at chosen octave count
                    v += a * noise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            void main() {
                vec2 p = uv;
                p.x *= resolution.x / resolution.y;

                float t = time * timeScale;
                vec2 q = vec2(fbm(p * pScale + t), fbm(p * pScale - t));
                float n = fbm(p * pScale + q * qScale);
                n = pow(n, 2.1);   // >1.0 biases toward color1 (purple)

                vec3 col = mix(color1, color2, n);
                //float bias = smoothstep(0.2, 1.0, pow(n, 1.5));  // gold only in top 70%
                //vec3 col = mix(color1, color2, bias);
                fragColor = vec4(col, 1);
            }`;

// Shader helper
function compile(type, source) {
    const s = gl.createShader(type);
    gl.shaderSource(s, source);
    gl.compileShader(s);
    if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
        console.error(gl.getShaderInfoLog(s));
    }
    return s;
}

// Compile & link
const vs = compile(gl.VERTEX_SHADER, vsSource);
const fs = compile(gl.FRAGMENT_SHADER, fsSource);
const prog = gl.createProgram();
gl.attachShader(prog, vs);
gl.attachShader(prog, fs);
gl.linkProgram(prog);
gl.useProgram(prog);

// Uniform locations
const timeLoc = gl.getUniformLocation(prog, "time");
const resLoc = gl.getUniformLocation(prog, "resolution");
const octLoc = gl.getUniformLocation(prog, "octaves");
const pScaleLoc = gl.getUniformLocation(prog, "pScale");
const qScaleLoc = gl.getUniformLocation(prog, "qScale");
const timeScaleLoc = gl.getUniformLocation(prog, "timeScale");
const color1Loc = gl.getUniformLocation(prog, "color1");
const color2Loc = gl.getUniformLocation(prog, "color2");

// Initial uniform values
gl.uniform2f(resLoc, canvas.width, canvas.height);
gl.uniform1i(octLoc, 5);
gl.uniform1f(pScaleLoc, 3.0);
gl.uniform1f(qScaleLoc, 5.1);
gl.uniform1f(timeScaleLoc, 0.03);
gl.uniform3f(color1Loc, 0.4, 0.0, 0.5);  // purple
gl.uniform3f(color2Loc, 1.0, 0.8, 0.3);  // gold

// Render loop
function render() {
    gl.uniform1f(timeLoc, performance.now() * 0.001);
    gl.drawArrays(gl.TRIANGLES, 0, 6);
    requestAnimationFrame(render);
}
requestAnimationFrame(render);

// Resize handling
window.addEventListener("resize", () => {
    canvas.width = Math.floor(window.innerWidth * scale);
    canvas.height = Math.floor(window.innerHeight * scale);
    gl.viewport(0, 0, canvas.width, canvas.height);
    gl.uniform2f(resLoc, canvas.width, canvas.height);
});