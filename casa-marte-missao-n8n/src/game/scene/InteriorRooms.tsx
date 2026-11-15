import {useRef,useState} from "react";
import {useFrame} from "@react-three/fiber";
import {MODULES} from "@/config/rooms";
import {BALANCE} from "@/config/balance";
import {runtime} from "@/game/runtime";
export function useNearbyInteriors(){const [ids,setIds]=useState<string[]>([]),current=useRef<string[]>([]);useFrame(()=>{const next=MODULES.filter(m=>m.shape!=="park"&&Math.hypot(runtime.x-m.x,runtime.z-m.z)-Math.max(m.width,m.depth)/2<(current.current.includes(m.id)?BALANCE.ambience.interiorFar:BALANCE.ambience.interiorNear)).map(m=>m.id);if(next.join()!==current.current.join()){current.current=next;setIds(next);}});return ids;}
