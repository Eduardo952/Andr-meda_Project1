import React, {lazy, Suspense} from 'react';
import {createRoot} from 'react-dom/client';
import '@fontsource/fredoka/600.css';
import '@fontsource/fredoka/700.css';
import '@fontsource/nunito/400.css';
import '@fontsource/nunito/800.css';
import './preview.css';
import {TitleScreen} from '@/ui/TitleScreen';
import {SolReport} from '@/ui/SolReport';
import {EnergyPanel} from '@/ui/EnergyPanel';
import {Hud} from '@/ui/Hud';
import {GreenhousePanel} from '@/ui/GreenhousePanel';
import {SettingsDialog} from '@/ui/SettingsDialog';
import {useGame} from '@/game/store';
import {useCampaign} from '@/game/campaign';
useGame.setState({lang:new URLSearchParams(location.search).get('lang')==='en'?'en':'pt-BR'});
const view=new URLSearchParams(location.search).get('view');
const taskId=new URLSearchParams(location.search).get('task') ?? 'TiltSun';
// Use the untouched task components; the remote NASA photo query is outside this offline preview.
const tasks = {
  TiltSun: lazy(() => import('@/tasks/TiltSun')),
  PipeFlow: lazy(() => import('@/tasks/PipeFlow')),
  WireShield: lazy(() => import('@/tasks/WireShield')),
  AlignCoils: lazy(() => import('@/tasks/AlignCoils')),
  CmeCountdown: lazy(() => import('@/tasks/CmeCountdown')),
  IrrigationPlan: lazy(() => import('@/tasks/IrrigationPlan')),
  SmartFarmCare: lazy(() => import('@/tasks/SmartFarmCare')),
  CleanPanels: lazy(() => import('@/tasks/CleanPanels')),
  SortFlares: lazy(() => import('@/tasks/SortFlares')),
  PowerBalance: lazy(() => import('@/tasks/PowerBalance')),
  ResourcePriority: lazy(() => import('@/tasks/ResourcePriority')),
  ThermostatDial: lazy(() => import('@/tasks/ThermostatDial')),
  WindReading: lazy(() => import('@/tasks/WindReading')),
};
const Task=tasks[taskId as keyof typeof tasks];
if(view) {useGame.setState({screen:'play',currentRoom:view==='garden'?'greenhouse':'control'});useCampaign.getState().start();if(view!=='report')useCampaign.getState().closeReport();}
import {SOLS} from '@/data/campaign';
function Review(){const screen=useGame(s=>s.screen),report=useCampaign(s=>s.reportOpen);return screen==='title'?<TitleScreen/>:<div data-game-root style={{height:'100dvh',background:'#a08c77'}}><Hud onSettings={()=>{}} onData={()=>{}}/>{view==='task'&&Task&&<Suspense><Task sol={SOLS[0]} onDone={()=>{}}/></Suspense>}{view==='garden'&&<GreenhousePanel/>}{view==='energy'?<EnergyPanel/>:view==='settings'?<SettingsDialog onClose={()=>{}} showHome/>:report?<SolReport/>:null}</div>;}
createRoot(document.getElementById('root')!).render(<Review/>);
