// Attribute-aware indexing: retain hard normals, UV seams and vertex colours.
// Removes only zero-area/duplicate triangles, never decimates a silhouette.
export function optimizeMesh(mesh) {
  const channels = [['vertices', 3], ['normals', 3], ['uv', 2], ['colors', 3]].filter(([key]) => mesh[key]?.length);
  const sourceVertices = mesh.vertices.length / 3, sourceTriangles = mesh.indices.length / 3;
  for (const [key, width] of channels) {
    if (mesh[key].length !== sourceVertices * width || !mesh[key].every(Number.isFinite)) throw Error(`${mesh.name}: invalid ${key}`);
  }
  const remap = new Map(), values = Object.fromEntries(channels.map(([key]) => [key, []]));
  const indices = [], faces = new Set(); let degenerate = 0, duplicate = 0;
  const p = i => mesh.vertices.slice(i * 3, i * 3 + 3);
  for (let i = 0; i < mesh.indices.length; i += 3) {
    const ids = mesh.indices.slice(i, i + 3);
    if (ids.length !== 3 || ids.some(n => !Number.isInteger(n) || n < 0 || n >= sourceVertices)) throw Error(`${mesh.name}: invalid triangle`);
    const [a, b, c] = ids.map(p), u = b.map((v,j) => v-a[j]), v = c.map((n,j) => n-a[j]);
    const cross = [u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];
    if (cross.reduce((s,n) => s+n*n,0) < 1e-20) { degenerate++; continue; }
    const mapped = ids.map(id => {
      const key = channels.flatMap(([name,width]) => mesh[name].slice(id*width,id*width+width)).join(',');
      if (!remap.has(key)) {
        remap.set(key, remap.size);
        for (const [name,width] of channels) values[name].push(...mesh[name].slice(id*width,id*width+width));
      }
      return remap.get(key);
    });
    // Cyclic equality preserves deliberately opposite-facing sheets.
    const k = mapped.indexOf(Math.min(...mapped));
    const face = [mapped[k],mapped[(k+1)%3],mapped[(k+2)%3]].join(',');
    if (faces.has(face)) { duplicate++; continue; }
    faces.add(face); indices.push(...mapped);
  }
  Object.assign(mesh, values, { indices });
  return { name: mesh.name, sourceVertices, vertices: mesh.vertices.length/3, sourceTriangles, triangles: indices.length/3, degenerate, duplicate };
}
