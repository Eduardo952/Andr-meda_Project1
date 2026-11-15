/**
 * Gameplay tunables. Values marked PROVISIONAL come from items marked ❓ (pending)
 * in the Mars Mission Trainer context document and must be confirmed by the project owner.
 * None of these values are NASA data.
 */
export const BALANCE = {
  // PROVISIONAL (❓ doc §7): how long one Sol lasts in real play time is not defined.
  realSecondsPerSol: 480,
  // PROVISIONAL (❓ doc §7): starting time of day for Sol 1 (fraction of the Sol, 0.3 ≈ early morning).
  startSolFraction: 0.3,
  // Campaign length: 5 Sols (✅ doc §1).
  campaignSols: 5,
  // PROVISIONAL (❓ doc §3 step 8 / I-14): initial Integrity and O2 are not defined.
  initialIntegrity: 100,
  initialOxygen: 100,
  // 🎮 GDD v1 (✅): 100 kWh per Sol. A game budget, not a real measurement (I-07).
  energyPool: 100,
  // PROVISIONAL: explicitly enabled simulated dust events; never a NASA dust-opacity measurement.
  marsWeatherModifier: true,
  sliderStep: 5,
  // ✅ GDD v1 margin: |allocated − demand| ≤ 5 is "ideal" (see I-13).
  margin: 5,
  // ✅ GDD v1 shield formula constants (with ⚠️ I-03/I-06/I-10 noted; CME term is 0 without a speed).
  shield: {
    flareBase: { A: 5, B: 5, C: 10, M: 22, X: 40 },
    flareMagnitudeFactor: 2.5,
    cmeReferenceSpeed: 400,
    cmeFactor: 4,
    min: 10,
    max: 75,
  },
  // PROVISIONAL (💡 doc §22.1, confirmed by owner brief): E_thermal = base + k·max(0, T_int − AT_mean).
  thermal: { base: 10, k: 0.15, internalC: 20, missingFallback: 25 },
  // PROVISIONAL (❓ I-08: τ has no source): constant greenhouse demand.
  greenhouse: { demand: 20 },
  // PROVISIONAL (❓ I-14): damage per missing kWh.
  damage: { integrityPerShieldKwh: 1, oxygenPerLifeKwh: 1, oxygenPerGreenhouseKwh: 0.5 },
  // PROVISIONAL (❓ doc §23): task rewards.
  taskReward: { integrity: 2, oxygen: 2 },
  // PROVISIONAL: task time limit and shelter run time (seconds of real time).
  taskSeconds: 60,
  // PROVISIONAL: tactile task rules; not scientific measurements.
  miniGames: {
    threeStarSeconds: 30, twoStarSeconds: 45, twoStarMistakes: 2,
    dialCount: 3, dialSteps: 12, dialTargets: [3, 6, 9],
    coilMarks: [4, 6, 12], coilMotionSeconds: [8, 6, 4],
    wireCount: 4, pipeCount: 6, beds: 3, apples: 3, lightTarget: 3,
    dustPatches: 12, weatherDustCap: 20, tiltStepDegrees: 5,
    speedStep: 10, speedTolerance: 50,
  },
  shelterSeconds: 60,
  // PROVISIONAL: deterministic training storms and dust-driven next-Sol energy loss.
  weather: { windStormThreshold: 12, scriptedStormSols: [2, 4] as readonly number[], dustGain: [55, 45, 35, 25] as readonly number[] },
  // PROVISIONAL: consequence severity, crew dose and perfect-Sol rules (game units, not real dose).
  consequences: { minorMissingRatio: 0.25, moderateMissingRatio: 0.6, dosePerShieldKwh: 0.8, shelterMissedDose: 35, severeDose: 75, perfectBonusStars: 3, incompleteTaskFraction: 0.25, goldResourceMin: 80, silverResourceMin: 50, goldDoseMax: 25, silverDoseMax: 60 },
  // PROVISIONAL: panel cleaning levels and tactile pass counts.
  cleaning: { lightMax: 35, heavyMax: 70, lightPasses: 1, heavyPasses: 2, buriedPasses: 3, animationSeconds: 6 },
  // PROVISIONAL: non-flashing alert presentation timing in seconds.
  alerts: { advisory: 1.4, warning: 2.8, emergency: 5.6, recovery: 8.6 },
  // PROVISIONAL: a flare at or above this class letter raises the alarm (with any CME speed or SEP).
  alarmFlareLetters: ["M", "X"] as readonly string[],
  // PROPOSAL (💡, not a decision): win = finish all 5 Sols with Integrity and O2 above 0.
  winRequiresAboveZero: true,
  // Optional tasks T09, T11, T12, T13 are off by default.
  optionalTasksEnabled: false,
  // PROVISIONAL: the narrator may reveal the ideal kWh values in the Sol summary.
  REVEAL_ANSWER_AT_SUMMARY: true,
  // Narrator proxy: request timeout (ms) and per-device rate limit.
  narratorTimeoutMs: 6000,
  narratorMaxPerMinute: 12,
  // 🎮 Adaptation: movement feel, not physics.
  walkSpeed: 5.2,
  walkAccel: 10,
  useRadius: 2.4,
  // 🎮 Camera rig offsets.
  cameraFollow: [0, 7.5, 10.5] as const,
  cameraIsometric: [13, 15, 13] as const,
  cameraDamping: 4,
  quality: { low: { pixelRatio: 1, bloom: false }, high: { pixelRatio: 1.5, bloom: true } },
  // PROPOSAL: four original crew identities (including the player) and decorative routines.
  crew: { proposal: true, count: 4, sleepUntil: 0.25, breakfastUntil: 0.34, workUntil: 0.7, exerciseUntil: 0.8, dinnerUntil: 0.9 },
  // Artistic ambience controls only; never NASA measurements or gameplay outcomes.
  ambience: { interiorNear: 12, interiorFar: 18, shellOpacity: 0.09, fadeDamping: 7, jumpSeconds: 1.2, celebrationSeconds: 3.5, fallbackWindYaw: 0.7 },
  // Camera controls are gameplay tunables, not scientific measurements.
  camera: {
    defaultYaw: 0,
    defaultPitchDegrees: 31,
    defaultDistance: 12.25,
    minPitchDegrees: 15,
    maxPitchDegrees: 70,
    minDistance: 7,
    maxDistance: 24,
    isometricYaw: Math.PI / 4,
    isometricPitchDegrees: 37,
    isometricDistance: 23,
    lookHeight: 1.2,
    rotationDamping: 10,
    zoomDamping: 8,
    radiansPerPixel: 0.005,
    wheelIntensity: 0.0015,
    minSensitivity: 0.25,
    maxSensitivity: 2,
    sensitivityStep: 0.05,
    defaultSensitivity: 1,
    groundClearance: 0.8,
    domeClearance: 0.5,
    domeHeightRatio: 0.75,
  },
} as const;
