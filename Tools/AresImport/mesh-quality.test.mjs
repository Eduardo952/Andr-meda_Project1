import test from 'node:test';
import assert from 'node:assert/strict';
import {optimizeMesh} from './mesh-quality.mjs';
test('shares exact vertices while preserving UV seams and hard edges',()=>{
  const m={name:'seams',vertices:[0,0,0,1,0,0,0,1,0,0,0,0,1,0,0,0,1,0],normals:[0,0,1,0,0,1,0,0,1,0,0,-1,0,0,-1,0,0,-1],uv:[0,0,1,0,0,1,.5,.5,1,0,0,1],indices:[0,1,2,3,5,4]};
  optimizeMesh(m);assert.equal(m.vertices.length/3,6);assert.equal(m.indices.length,6);assert.equal(m.uv[6],.5);
});
test('removes collapsed and duplicated faces but keeps a two-sided sheet',()=>{
  const m={name:'sheet',vertices:[0,0,0,1,0,0,0,1,0,0,0,0],normals:[],uv:[],indices:[0,1,2,3,1,2,2,1,0,0,0,1]};
  const stats=optimizeMesh(m);assert.equal(stats.degenerate,1);assert.equal(stats.duplicate,1);assert.equal(m.indices.length,6);assert.equal(m.vertices.length/3,3);
});
test('invalid input fails rather than silently creating a hole',()=>{
  assert.throws(()=>optimizeMesh({name:'bad',vertices:[0,0,0],indices:[0,1,2]}),/invalid triangle/);
  assert.throws(()=>optimizeMesh({name:'bad',vertices:[NaN,0,0],indices:[]}),/invalid vertices/);
});
