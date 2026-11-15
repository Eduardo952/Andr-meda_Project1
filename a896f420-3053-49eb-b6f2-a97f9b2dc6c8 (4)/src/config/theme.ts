/** Shared art tokens. The corresponding interface roles live in styles.css. */
export const THEME = {
  hull: "#fff8ec", floor: "#ead5b3", floorSeam: "#c6ae89", terrain: "#c8582f",
  trim: "#c3c9c4", ink: "#34323b", visor: "#365568", glass: "#c5e7e3",
  face: "#ffe1aa", solarCell: "#355468",
  geology: { soil: "#b75c3b", clay: "#ca7950", sand: "#d69b68", basalt: "#72483e", mineral: "#e1b483", dust: "#d9aa7c" },
  lighting: { dayFog: "#ddb28c", nightFog: "#282638", coolFill: "#b8dfeb", warmFill: "#f3c391", alarm: "#ef7f69" },
  emergency: { advisory: "#f4ca54", warning: "#ef9b54", critical: "#e65149", recovery: "#73b950", frost: "#b8dfeb", storm: "#ca7950" },
  garden: { leaf: "#73b950", deepLeaf: "#3e8659", tenderLeaf: "#a7ce69", tiredLeaf: "#c1b66d", apple: "#e65149", tomato: "#ef7650", trunk: "#a67e57", nutrient: "#67c8be", ledWarm: "#ef9ed5", ledCold: "#889bb8", flower: "#f7d68a", mist: "#e1eeeb" },
  regions: { control: "#ef7f69", observatory: "#a287da", shield: "#62a9e5", lifeSupport: "#3dbfb3", greenhouse: "#83bf64", solar: "#f4ca54", airlock: "#ef7f69" },
  materials: { toonTones: [95, 180, 255], hullRoughness: .8, visorRoughness: .14, visorMetalness: .65, stationGlow: .35 },
} as const;