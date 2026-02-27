export type SetupOption = {
    value: string;
    label: string;
    description: string;
};

export const CLASSIC_CITY_SIZE_TIER_OPTIONS: SetupOption[] = [
    {
        value: "Small",
        label: "Small footprint",
        description: "Compact morphology with tighter district planning around the requested launch population.",
    },
    {
        value: "Medium",
        label: "Balanced footprint",
        description: "General-purpose city form that balances district spread, building count, and bootstrap readability.",
    },
    {
        value: "Large",
        label: "Large footprint",
        description: "Broader urban footprint with more districts and looser distribution for the same population target.",
    },
];

export const CLASSIC_CITY_FORM_PRESET_OPTIONS: SetupOption[] = [
    {
        value: "CompactGrid",
        label: "Compact grid",
        description: "Smaller footprint with balanced density. Good for readable launches where people stay close to the center.",
    },
    {
        value: "BalancedDistricts",
        label: "Balanced districts",
        description: "General-purpose city form with room for both central activity and outer neighborhood growth.",
    },
    {
        value: "VerticalCore",
        label: "Vertical core",
        description: "Denser, more advanced launch shape that leans into towers and stronger central concentration.",
    },
    {
        value: "SprawlingSuburbs",
        label: "Sprawling suburbs",
        description: "Broader footprint with sparser housing distribution and more outer-ring residential character.",
    },
    {
        value: "PressureCooker",
        label: "Pressure cooker",
        description: "Tighter, rougher launch form with strong housing pressure and less comfortable headroom around the same population.",
    },
];

export const CLASSIC_CITY_DENSITY_OPTIONS: SetupOption[] = [
    {
        value: "Sparse",
        label: "Sparse",
        description: "Lower-rise residential mix with more spread-out neighborhood planning and softer housing pressure.",
    },
    {
        value: "Balanced",
        label: "Balanced",
        description: "Middle-ground urban form for a readable launch without extreme housing slack or compression.",
    },
    {
        value: "Dense",
        label: "Dense",
        description: "Denser residential mix with taller housing stock and stronger launch pressure around the target headcount.",
    },
];

export const CLASSIC_CITY_DEVELOPMENT_OPTIONS: SetupOption[] = [
    {
        value: "Struggling",
        label: "Struggling",
        description: "Leans toward rougher launch conditions, tighter housing coverage, and more bootstrap pressure.",
    },
    {
        value: "Balanced",
        label: "Balanced",
        description: "Neutral development baseline for a stable day-one city bootstrap.",
    },
    {
        value: "Advanced",
        label: "Advanced",
        description: "Favors stronger housing stock, more efficient topology generation, and more launch headroom.",
    },
];

export const CLASSIC_CITY_POPULATION_TARGET_OPTIONS: SetupOption[] = [
    {
        value: "Random",
        label: "Randomized",
        description: "Use the generation seed to pick a deterministic launch headcount, so the same seed reproduces the same opening scale.",
    },
    {
        value: "Preset1K",
        label: "1,000 residents",
        description: "Small opening town with a fast bootstrap and a compact early-world snapshot.",
    },
    {
        value: "Preset10K",
        label: "10,000 residents",
        description: "Balanced default launch with enough citizens to show housing, employment, and wellbeing dynamics immediately.",
    },
    {
        value: "Preset100K",
        label: "100,000 residents",
        description: "Heavy launch aimed at a genuinely large city opening with broad topology generation and a dense bootstrap pass.",
    },
    {
        value: "Manual",
        label: "Manual target",
        description: "Enter an exact opening headcount when you want the launch contract to pin a specific population number.",
    },
];

export const CLASSIC_CITY_CLIMATE_OPTIONS: SetupOption[] = [
    {
        value: "Temperate",
        label: "Temperate",
        description: "Moderate seasonal profile and the safest general-purpose weather baseline.",
    },
    {
        value: "Continental",
        label: "Continental",
        description: "Sharper seasonal swings with stronger hot-cold contrast.",
    },
    {
        value: "Tropical",
        label: "Tropical",
        description: "Warm, humid baseline with heavier weather exposure.",
    },
    {
        value: "Arid",
        label: "Arid",
        description: "Dry climate profile with hotter daytime states and lower humidity pressure.",
    },
    {
        value: "Mountain",
        label: "Mountain",
        description: "Cooler, elevated weather baseline with rougher climate transitions.",
    },
    {
        value: "Polar",
        label: "Polar",
        description: "Cold-start climate profile with harsh seasonal stressors.",
    },
];

export const CLASSIC_CITY_HEMISPHERE_OPTIONS: SetupOption[] = [
    {
        value: "Northern",
        label: "Northern",
        description: "Season planner follows northern-hemisphere month ordering.",
    },
    {
        value: "Southern",
        label: "Southern",
        description: "Season planner mirrors weather progression for the southern hemisphere.",
    },
];

export const CLASSIC_CITY_INITIAL_WEATHER_MODE_OPTIONS: SetupOption[] = [
    {
        value: "Random",
        label: "Seeded random",
        description: "CityCore derives a deterministic starting weather state from the launch seed, climate setup, and start time.",
    },
    {
        value: "Manual",
        label: "Manual opening",
        description: "Pin the very first weather state yourself, then let normal weather simulation continue from there.",
    },
];

export const CLASSIC_CITY_INITIAL_WEATHER_TYPE_OPTIONS: SetupOption[] = [
    {
        value: "Clear",
        label: "Clear",
        description: "Dry opening with open sky and minimal cloud cover.",
    },
    {
        value: "Overcast",
        label: "Overcast",
        description: "Heavy cloud cover without active precipitation.",
    },
    {
        value: "Rain",
        label: "Rain",
        description: "Wet opening with drizzle or steady rainfall depending on severity.",
    },
    {
        value: "Snow",
        label: "Snow",
        description: "Cold opening with snow or sleet depending on temperature.",
    },
    {
        value: "Storm",
        label: "Storm",
        description: "Aggressive opening with stronger wind and severe precipitation.",
    },
    {
        value: "Fog",
        label: "Fog",
        description: "Low-visibility opening with saturated air and muted wind.",
    },
    {
        value: "Windy",
        label: "Windy",
        description: "Dryer opening driven by stronger surface wind.",
    },
    {
        value: "Heatwave",
        label: "Heatwave",
        description: "Hot opening biased toward high temperature and low cloud cover.",
    },
    {
        value: "ColdSnap",
        label: "Cold snap",
        description: "Cold opening with pressure and temperature shifted downward.",
    },
];

export const CLASSIC_CITY_INITIAL_WEATHER_SEVERITY_OPTIONS: SetupOption[] = [
    {
        value: "Calm",
        label: "Calm",
        description: "Softest opening state with minimal force behind the selected weather type.",
    },
    {
        value: "Mild",
        label: "Mild",
        description: "Default readable launch condition without pushing extremes too hard.",
    },
    {
        value: "Moderate",
        label: "Moderate",
        description: "Stronger opening with more visible impact on wind, clouds, and precipitation.",
    },
    {
        value: "Severe",
        label: "Severe",
        description: "Harsh opening intended for difficult or dramatic scenario starts.",
    },
    {
        value: "Extreme",
        label: "Extreme",
        description: "Maximum opening intensity when you want day one to begin under real stress.",
    },
];

export const CLASSIC_CITY_POPULATION_OCCUPANCY_OPTIONS: SetupOption[] = [
    {
        value: "Light",
        label: "Room to grow",
        description: "Builds extra housing headroom around the opening population so the city starts with visible spare capacity.",
    },
    {
        value: "Balanced",
        label: "Balanced housing",
        description: "Keeps launch population and housing stock close enough for a lively start without immediate saturation.",
    },
    {
        value: "High",
        label: "Tight housing",
        description: "Builds a tighter city around the same headcount, increasing launch pressure and the odds of homelessness emerging immediately.",
    },
];
