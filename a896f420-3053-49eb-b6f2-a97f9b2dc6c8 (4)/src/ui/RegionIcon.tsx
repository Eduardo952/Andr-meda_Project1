import { Telescope, Shield, Cylinder, Sprout, PanelsTopLeft, Zap, DoorOpen } from "lucide-react";
import type { RoomId } from "@/config/rooms";
const ICONS = { control: Zap, observatory: Telescope, shield: Shield, lifeSupport: Cylinder, greenhouse: Sprout, solar: PanelsTopLeft, airlock: DoorOpen };
export function RegionIcon({ room, size = 20 }: { room: RoomId; size?: number }) { const Icon = ICONS[room]; return <Icon width={size} height={size} />; }