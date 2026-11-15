import * as THREE from "three";
import { THEME } from "@/config/theme";
import { seeded } from "./materials";

/** Small locally generated maps; no downloads or texture-loading suspense. */
export function regolithMaps() {
  const size = 512, canvas = document.createElement("canvas"), bump = document.createElement("canvas");
  canvas.width = canvas.height = bump.width = bump.height = size;
  const context = canvas.getContext("2d"), relief = bump.getContext("2d");
  if (!context || !relief) return { map: new THREE.Texture(), bumpMap: new THREE.Texture() };
  const image = context.createImageData(size, size), height = relief.createImageData(size, size), random = seeded(107);
  const dark = new THREE.Color(THEME.geology.soil), light = new THREE.Color(THEME.geology.clay);
  // Convert palette roles to canvas sRGB bytes rather than treating linear values as color bytes.
  dark.convertLinearToSRGB(); light.convertLinearToSRGB();
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const u = x / size * Math.PI * 2, v = y / size * Math.PI * 2;
    const grain = random(), wave = Math.sin(u * 2 + Math.sin(v)) * Math.cos(v * 3) * .16 + Math.sin(u * 7 + v * 5) * .06;
    const blend = Math.max(0, Math.min(1, .5 + wave + (grain - .5) * .23));
    const i = (y * size + x) * 4;
    image.data[i] = (dark.r + (light.r - dark.r) * blend) * 255;
    image.data[i + 1] = (dark.g + (light.g - dark.g) * blend) * 255;
    image.data[i + 2] = (dark.b + (light.b - dark.b) * blend) * 255; image.data[i + 3] = 255;
    height.data[i] = height.data[i + 1] = height.data[i + 2] = 105 + grain * 65 + wave * 70; height.data[i + 3] = 255;
  }
  context.putImageData(image, 0, 0); relief.putImageData(height, 0, 0);
  // Low-contrast wind combs and mineral grains remain legible at normal play distance.
  context.strokeStyle = THEME.geology.sand; context.lineWidth = 2; context.globalAlpha = .16;
  for(let band=0;band<28;band++){context.beginPath();for(let x=0;x<=size;x+=8){const y=band*20+Math.sin(x/size*Math.PI*4+band*.35)*10;if(x===0)context.moveTo(x,y);else context.lineTo(x,y);}context.stroke();}
  for(let i=0;i<2100;i++){context.globalAlpha=.08+random()*.16;context.fillStyle=i%3===0?THEME.geology.basalt:THEME.geology.mineral;const s=.5+random()*2;context.fillRect(random()*size,random()*size,s,s*.6);}
  context.globalAlpha=1;
  const map = new THREE.CanvasTexture(canvas), bumpMap = new THREE.CanvasTexture(bump);
  for (const texture of [map, bumpMap]) { texture.wrapS = texture.wrapT = THREE.RepeatWrapping; texture.repeat.set(22, 22); texture.anisotropy = 4; }
  map.colorSpace = THREE.SRGBColorSpace;
  return { map, bumpMap };
}

export function solarCellMap() {
  const canvas = document.createElement("canvas"); canvas.width = canvas.height = 256;
  const g = canvas.getContext("2d"); if (!g) return new THREE.Texture();
  g.fillStyle = THEME.solarCell; g.fillRect(0, 0, 256, 256);
  for (let row = 0; row < 6; row++) for (let col = 0; col < 4; col++) {
    g.fillStyle = THEME.visor; g.fillRect(col * 64 + 3, row * 42 + 3, 58, 36);
    g.strokeStyle = THEME.trim; g.globalAlpha = .45; g.lineWidth = 1;
    for (let line = 1; line <= 3; line++) { g.beginPath(); g.moveTo(col * 64 + line * 15, row * 42 + 3); g.lineTo(col * 64 + line * 15, row * 42 + 39); g.stroke(); }
    g.globalAlpha = 1;
  }
  const texture = new THREE.CanvasTexture(canvas); texture.colorSpace = THREE.SRGBColorSpace; texture.anisotropy = 4; return texture;
}