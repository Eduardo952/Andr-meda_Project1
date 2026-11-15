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
// Radial layout: a round hub with five satellites at equal 72-degree spacing. Every satellite tube spans the
// same radial distance (hub wall to satellite wall), so corridors share one length and width.
const r2=(v:number)=>Math.round(v*100)/100;
/** Polar helper on the ground plane, using the same angle convention as COMMAND_CONSOLE (x=sin, z=cos). */
export const polar=(deg:number,r:number):[number,number]=>[r2(Math.sin(deg*Math.PI/180)*r),r2(Math.cos(deg*Math.PI/180)*r)];
export const HUB_RADIUS=14, CORRIDOR_LENGTH=18, SATELLITE_RADIUS=13;
export const SATELLITE_ANGLES={greenhouse:90,observatory:162,shield:234,solar:306,lifeSupport:18} as const;
const SAT_CENTER=HUB_RADIUS+CORRIDOR_LENGTH+SATELLITE_RADIUS;
const dome=(id:RoomId,wing:Wing,color:string)=>{const a=SATELLITE_ANGLES[id as keyof typeof SATELLITE_ANGLES];const [x,z]=polar(a,SAT_CENTER);return module(id,wing,"dome",x,z,SATELLITE_RADIUS*2,SATELLITE_RADIUS*2,6,color,polar(a,SAT_CENTER+4),[polar(a,HUB_RADIUS+CORRIDOR_LENGTH)]);};
const SOLAR_A=SATELLITE_ANGLES.solar;
/** The airlock sits at the end of the solar tube; the outdoor field starts beyond it. */
export const AIRLOCK_CENTER=HUB_RADIUS+CORRIDOR_LENGTH+4, SOLAR_CENTER=60;
/** Maps the Solar Park's original local frame (field centred at 0,82, access from -z) into the rotated radial frame. */
export function solarFrame(x:number,z:number):[number,number]{const a=SOLAR_A*Math.PI/180,[cx,cz]=polar(SOLAR_A,SOLAR_CENTER),lz=z-82;return[r2(cx+x*Math.cos(a)+lz*Math.sin(a)),r2(cz-x*Math.sin(a)+lz*Math.cos(a))];}
export const SOLAR_YAW=SOLAR_A*Math.PI/180;
export const MODULES:ModuleDef[]=[
 module("control","hub","hub",0,0,HUB_RADIUS*2,HUB_RADIUS*2,6.5,P.coral,[8,-8],(Object.values(SATELLITE_ANGLES) as number[]).map(a=>polar(a,HUB_RADIUS))),
 dome("observatory","work",P.techPurple),
 dome("shield","shield",P.skyBlue),
 module("greenhouse","greenhouse","glass",52,0,40,32,12,P.leaf,[48,0],[[32,0]]),
 dome("lifeSupport","technical",P.aqua),
 module("airlock","technical","dome",...polar(SOLAR_A,AIRLOCK_CENTER),8,8,4,P.orange,polar(SOLAR_A,AIRLOCK_CENTER),[polar(SOLAR_A,AIRLOCK_CENTER-4),polar(SOLAR_A,AIRLOCK_CENTER+4)]),
 module("solar","source","park",...polar(SOLAR_A,SOLAR_CENTER),44,44,0,P.sunYellow,solarFrame(0,76),[])
];
export const REGIONS:RoomId[]=["observatory","shield","lifeSupport","greenhouse","solar"];
export const ROOMS:RoomDef[]=MODULES.filter((m):m is ModuleDef & {room:RoomId}=>!!m.room).map(m=>({...m,id:m.room,radius:Math.max(m.width,m.depth)/2}));
export const ROOM_BY_ID=Object.fromEntries(ROOMS.map(r=>[r.id,r])) as Record<RoomId,RoomDef>;
export const TOUR_ORDER:RoomId[]=["control",...REGIONS,"airlock"];
export const CORRIDOR_HALF_WIDTH=2.5;
const link=(ax:number,az:number,bx:number,bz:number,width=5):PathLink=>({a:{x:ax,z:az},b:{x:bx,z:bz},width});
const tube=(a:number)=>link(...polar(a,HUB_RADIUS-1),...polar(a,HUB_RADIUS+CORRIDOR_LENGTH+1));
export const CORRIDORS=(Object.values(SATELLITE_ANGLES) as number[]).map(tube);
export const PATHS=[...CORRIDORS,link(...polar(SOLAR_A,AIRLOCK_CENTER+3),...solarFrame(0,83),6),link(...solarFrame(-23,82),...solarFrame(23,82),25)];
export const INTERIOR_PATHS:PathLink[]=MODULES.flatMap(m=>[...m.ports.map(([x,z])=>link(x,z,m.x,m.z)),link(m.x,m.z,...m.entrance)]);
export const SPAWN={x:0,z:4};
export const MAP_BOUNDS={minX:-80,maxX:78,minZ:-62,maxZ:66};
/** Outdoor landmarks outside the ring; terrain keeps them flat and rock-free. */
export const LANDMARKS={shieldBubble:84,generator:{p:polar(198,74),r:3},landingPad:{p:polar(126,100),r:11},rover:{p:polar(342,92),r:5},mast:{p:polar(270,60),r:2}} as const;
export function doorway(m:ModuleDef){const [x,z]=m.ports[0]??[m.x,m.z+m.depth/2];const axisX=Math.abs(x-m.x)>Math.abs(z-m.z);const sign=Math.sign(axisX?x-m.x:z-m.z);const l=Math.hypot(x-m.x,z-m.z)||1;return{x,z,axisX,sign,dir:{x:(x-m.x)/l,z:(z-m.z)/l},outside:{x:x+(x-m.x)/l,z:z+(z-m.z)/l},halfWidth:2.5};}
/** Control Room wall console: one angle drives desk, levers and screen so they stay aligned. */
export const COMMAND_CONSOLE={angle:Math.PI*.75,deskRadius:11.6,leverRadius:11.05,leverY:2.05,leverGap:1.25,screenRadius:12.75,screenY:3.85,seatRadius:9.1,seatGap:1.65,seatBackOffset:.5};
export const consolePoint=(radius:number,tangent=0):[number,number]=>{const a=COMMAND_CONSOLE.angle;return[Math.sin(a)*radius+Math.cos(a)*tangent,Math.cos(a)*radius-Math.sin(a)*tangent];};
export const consoleYaw=COMMAND_CONSOLE.angle+Math.PI;
