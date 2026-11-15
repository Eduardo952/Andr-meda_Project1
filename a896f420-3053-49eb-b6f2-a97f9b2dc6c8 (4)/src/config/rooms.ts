import { PALETTE as P } from "./palette";
export type RoomId = "control" | "shield" | "lifeSupport" | "greenhouse" | "observatory" | "solar" | "airlock";
export type Wing = "hub" | "shield" | "work" | "technical" | "greenhouse" | "source";
export type ModuleShape = "hub" | "dome" | "barrel" | "box" | "glass" | "park";
export interface ModuleDef { id: string; room?: RoomId; wing: Wing; shape: ModuleShape; x: number; z: number; width: number; depth: number; height: number; color: string; entrance: [number,number]; ports: [number,number][] }
export interface RoomDef extends ModuleDef { id: RoomId; room: RoomId; radius: number }
export interface PathPoint { x:number; z:number }
export interface PathLink { a:PathPoint; b:PathPoint; width:number }
const module=(id:RoomId,wing:Wing,shape:ModuleShape,x:number,z:number,width:number,depth:number,height:number,color:string,entrance:[number,number],ports:[number,number][]):ModuleDef=>({id,room:id,wing,shape,x,z,width,depth,height,color,entrance,ports});
// Artistic dimensions, never NASA engineering measurements. Airlock is transit, not a region.
export const MODULES:ModuleDef[]=[
 module("control","hub","hub",0,0,28,28,6.5,P.coral,[8,-8],[[0,-14],[14,0],[0,14],[-14,0]]),
 module("observatory","work","barrel",0,-42,24,18,6,P.techPurple,[0,-46],[[0,-33]]),
 module("shield","shield","barrel",-44,0,26,18,6,P.skyBlue,[-48,0],[[-31,0]]),
 module("greenhouse","greenhouse","glass",52,0,40,32,12,P.leaf,[48,0],[[32,0]]),
 module("lifeSupport","technical","barrel",0,42,26,18,6,P.aqua,[0,45],[[0,33],[0,51]]),
 module("airlock","technical","box",0,57,6,10,4,P.orange,[0,57],[[0,52],[0,62]]),
 module("solar","source","park",0,82,48,26,0,P.sunYellow,[0,76],[])
];
export const REGIONS:RoomId[]=["observatory","shield","lifeSupport","greenhouse","solar"];
export const ROOMS:RoomDef[]=MODULES.filter((m):m is ModuleDef & {room:RoomId}=>!!m.room).map(m=>({...m,id:m.room,radius:Math.max(m.width,m.depth)/2}));
export const ROOM_BY_ID=Object.fromEntries(ROOMS.map(r=>[r.id,r])) as Record<RoomId,RoomDef>;
export const TOUR_ORDER:RoomId[]=["control",...REGIONS,"airlock"];
export const CORRIDOR_HALF_WIDTH=2.5;
const link=(ax:number,az:number,bx:number,bz:number,width=5):PathLink=>({a:{x:ax,z:az},b:{x:bx,z:bz},width});
export const CORRIDORS=[link(0,-13,0,-34),link(-13,0,-32,0),link(13,0,33,0),link(0,13,0,34),link(0,50,0,53)];
export const PATHS=[...CORRIDORS,link(0,61,0,83,6),link(-23,82,23,82,25)];
export const INTERIOR_PATHS:PathLink[]=MODULES.flatMap(m=>[...m.ports.map(([x,z])=>link(x,z,m.x,m.z)),link(m.x,m.z,...m.entrance)]);
export const SPAWN={x:0,z:4};
export const MAP_BOUNDS={minX:-62,maxX:78,minZ:-58,maxZ:100};
export function doorway(m:ModuleDef){const [x,z]=m.ports[0]??[m.x,m.z+m.depth/2];const axisX=Math.abs(x-m.x)>Math.abs(z-m.z);const sign=Math.sign(axisX?x-m.x:z-m.z);return{x,z,axisX,sign,outside:{x:x+(axisX?sign:0),z:z+(axisX?0:sign)},halfWidth:2.5};}
/** Control Room wall console: one angle drives desk, levers and screen so they stay aligned. */
export const COMMAND_CONSOLE={angle:Math.PI*.75,deskRadius:11.6,leverRadius:11.05,leverY:2.05,leverGap:1.25,screenRadius:12.75,screenY:3.85,seatRadius:9.1,seatGap:1.65,seatBackOffset:.5};
export const consolePoint=(radius:number,tangent=0):[number,number]=>{const a=COMMAND_CONSOLE.angle;return[Math.sin(a)*radius+Math.cos(a)*tangent,Math.cos(a)*radius-Math.sin(a)*tangent];};
export const consoleYaw=COMMAND_CONSOLE.angle+Math.PI;
