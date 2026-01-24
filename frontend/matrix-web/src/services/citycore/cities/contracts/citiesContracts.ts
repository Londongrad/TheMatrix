//src/services/citycore/cities/contracts/citiesContracts.ts
export interface CityListItemView {
  cityId: string;
  name: string;
  status: string;
}

export interface CreateCityRequest {
  name: string;
  startSimTimeUtc: string;
  speedMultiplier: number;
}

export interface CityCreatedView {
  cityId: string;
}

export interface CityView {
  cityId: string;
  name: string;
  status: string;
  createdAtUtc: string;
  archivedAtUtc?: string | null;
}

export interface RenameCityRequest {
  name: string;
}
