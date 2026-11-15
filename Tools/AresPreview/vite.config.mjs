import {defineConfig} from 'vite';
import react from '@vitejs/plugin-react';
import tailwind from '@tailwindcss/vite';
import path from 'node:path';
import fs from 'node:fs';
const source=path.resolve(import.meta.dirname,'../../casa-marte-missao-n8n/src');
export default defineConfig({plugins:[{name:'original-styles',enforce:'pre',transform(code,id){if(id.endsWith('/preview.css'))return code+'\n'+fs.readFileSync(path.join(source,'styles.css'),'utf8').replace(/^@(?:import|source)[^;]+;\r?\n/gm,'');}},react(),tailwind()],resolve:{alias:[
 {find:'@/services/narrator/speak',replacement:path.resolve(import.meta.dirname,'narrator.ts')},
 {find:'@',replacement:source},
 ...['react','react-dom','three','@react-three/fiber','@react-three/drei','lucide-react','zustand','@radix-ui/react-slot','@radix-ui/react-dialog','class-variance-authority','clsx','tailwind-merge'].map(name=>({find:name,replacement:path.resolve(import.meta.dirname,'node_modules',name)}))
]},server:{fs:{allow:[path.resolve(import.meta.dirname,'../..')]}}});
