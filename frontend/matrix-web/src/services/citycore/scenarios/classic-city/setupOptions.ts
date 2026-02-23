export type SetupOption = {
    value: string;
    label: string;
    description: string;
};

export const CLASSIC_CITY_SIZE_TIER_OPTIONS: SetupOption[] = [
    {
        value: "Small",
        label: "Small footprint",
        description: "Compact seed with fewer districts and a faster path to a readable simulation snapshot.",
    },
    {
        value: "Medium",
        label: "Balanced footprint",
        description: "General-purpose launch profile with room for population bootstrap and monitoring without oversized startup cost.",
    },
    {
        value: "Large",
        label: "Large footprint",
        description: "Broader district layout with more residential capacity and a heavier bootstrap pass.",
    },
];

export const CLASSIC_CITY_DENSITY_OPTIONS: SetupOption[] = [
    {
        value: "Sparse",
        label: "Sparse",
        description: "Lower density and softer occupancy assumptions for a more spread-out city seed.",
    },
    {
        value: "Balanced",
        label: "Balanced",
        description: "Middle-ground density profile tuned for the current default Classic City flow.",
    },
    {
        value: "Dense",
        label: "Dense",
        description: "Higher residential pressure, tighter district planning, and a stronger bootstrap population target.",
    },
];

export const CLASSIC_CITY_DEVELOPMENT_OPTIONS: SetupOption[] = [
    {
        value: "Struggling",
        label: "Struggling",
        description: "Leans toward weaker city infrastructure and more constrained launch capacity.",
    },
    {
        value: "Balanced",
        label: "Balanced",
        description: "Neutral development baseline for day-one simulation monitoring.",
    },
    {
        value: "Advanced",
        label: "Advanced",
        description: "Favors stronger topology generation and a more capable starting environment.",
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
