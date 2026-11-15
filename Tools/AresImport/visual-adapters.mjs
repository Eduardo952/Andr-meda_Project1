// Preserve the original builders and geometry, but retain boundaries around movable props.
// These adapters operate on an in-memory copy; the reference tree is never edited.
export function movableProps(file, source) {
  if (file === 'game/scene/Greenhouse.tsx') {
    const tankStart=source.indexOf('  // Water reservoir at the far end');
    const tankEnd=source.indexOf('  // Manual watering can',tankStart);
    if(tankStart<0||tankEnd<0)throw Error('Reservoir source changed; update its adapter.');
    const tank=source.slice(tankStart,tankEnd).replace(/\bb\./g,'reservoir.');
    source=source.slice(0,tankStart)+'  const reservoir=new Construction(high);\n'+tank
      +'  reservoir.finish(); reservoir.group.userData.role="water_reservoir"; b.group.add(reservoir.group);\n'+source.slice(tankEnd);
    const start = source.indexOf('  b.cylinder([35.2, .9');
    const end = source.indexOf('  // Rear tomato', start);
    if (start < 0 || end < 0) throw Error('Watering can source changed; update its adapter.');
    const body = source.slice(start, end).replace(/\bb\./g, 'wateringCan.');
    source = source.slice(0, start) + '  const wateringCan = new Construction(high);\n' + body
      + '  wateringCan.finish(); wateringCan.group.userData.role="watering_can"; b.group.add(wateringCan.group);\n' + source.slice(end);
  }
  if (file === 'game/scene/BaseLandmarks.tsx') {
    const start = source.indexOf('  // Rover parked near the edge.');
    const end = source.indexOf('  b.finish(); return b;', start);
    if (start < 0 || end < 0) throw Error('Rover source changed; update its adapter.');
    let body = source.slice(start, end).replace(/\bb\./g, 'rover.');
    body = body.replace(/for \(const sx of \[-1\.45, 1\.45\]\) for \(const sz of \[-1\.4, 0, 1\.4\]\) rover\.cylinder\(([^;]+)\);/,
      'let wheelIndex=0; for (const sx of [-1.45,1.45]) for (const sz of [-1.4,0,1.4]) { const wheel=new Construction(high); wheel.cylinder($1); wheel.finish(); wheel.group.userData.role="rover_wheel"; wheel.group.traverse(o=>{if(o.isMesh)o.userData.instance=wheelIndex;}); wheelIndex++; rover.group.add(wheel.group); }');
    source = source.slice(0, start) + '  const rover = new Construction(high);\n' + body
      + '  rover.finish(); rover.group.userData.role="reference_rover"; b.group.add(rover.group);\n' + source.slice(end);
  }
  return source;
}

export function gardenLayout(meshes) {
  const bounds=m=>{
    const min=[Infinity,Infinity,Infinity],max=[-Infinity,-Infinity,-Infinity];
    for(let i=0;i<m.vertices.length;i++) { const axis=i%3;min[axis]=Math.min(min[axis],m.vertices[i]);max[axis]=Math.max(max[axis],m.vertices[i]); }
    return {min,max};
  };
  const point=role=>{
    const matching=meshes.filter(m=>m.role===role); if(!matching.length)throw Error('Missing '+role);
    const boxes=matching.map(bounds),min=[0,1,2].map(i=>Math.min(...boxes.map(b=>b.min[i]))),max=[0,1,2].map(i=>Math.max(...boxes.map(b=>b.max[i])));
    return {x:(min[0]+max[0])/2,z:(min[2]+max[2])/2,height:max[1]};
  };
  const containers=meshes.filter(m=>m.role==='cultivation_container'),rows=new Map();
  for(const container of containers){const b=bounds(container),z=Math.round((b.min[2]+b.max[2])*50)/100;if(!rows.has(z))rows.set(z,[]);rows.get(z).push(b);}
  if(rows.size!==4||containers.length!==40)throw Error('Expected four shelves / forty containers. Update care binding deliberately.');
  return {pickup:point('watering_can'),refill:point('water_reservoir'),rows:[...rows].sort((a,b)=>a[0]-b[0]).map(([z,boxes])=>{
    const left=Math.min(...boxes.map(b=>b.min[0])),right=Math.max(...boxes.map(b=>b.max[0]));
    return {x:(left+right)/2,z,width:right-left,depth:boxes[0].max[2]-boxes[0].min[2],containers:boxes.length};
  })};
}
