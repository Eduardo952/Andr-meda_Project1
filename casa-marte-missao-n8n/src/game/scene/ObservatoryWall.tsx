import { Html } from "@react-three/drei";
import { QueryClientProvider, useQueryClient } from "@tanstack/react-query";
import { NasaCards } from "@/ui/NasaCards";
export function ObservatoryWall(){const client=useQueryClient();return <Html position={[0,3,-48.4]} transform distanceFactor={9} occlude="blending" zIndexRange={[2,0]}><QueryClientProvider client={client}><div className="wall-display wall-nasa"><NasaCards variant="wall"/></div></QueryClientProvider></Html>;}
