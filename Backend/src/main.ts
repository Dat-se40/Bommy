var SERVER_VERSION = "0.2.0";
var PROGRESSION_COLLECTION = "player_progression";
var PROGRESSION_KEY = "state";
var PROGRESSION_SCHEMA_VERSION = 1;
var PROGRESSION_WRITE_RETRIES = 3;

interface CharacterCatalogEntry {
  characterId: number;
  price: number;
  requiredLevel: number;
}

interface PlayerProgression {
  schemaVersion: number;
  gold: number;
  experience: number;
  level: number;
  ownedCharacterIds: number[];
  selectedCharacterId: number;
}

interface CharacterRequest {
  characterId: number;
}

var CHARACTER_CATALOG: {[characterId: string]: CharacterCatalogEntry} = {
  "1": { characterId: 1, price: 0, requiredLevel: 1 },
  "2": { characterId: 2, price: 50, requiredLevel: 2 },
  "3": { characterId: 3, price: 0, requiredLevel: 1 },
  "4": { characterId: 4, price: 0, requiredLevel: 1 },
  "5": { characterId: 5, price: 0, requiredLevel: 1 }
};

function defaultProgression(): PlayerProgression {
  return {
    schemaVersion: PROGRESSION_SCHEMA_VERSION,
    gold: 850,
    experience: 0,
    level: 1,
    ownedCharacterIds: [1],
    selectedCharacterId: 1
  };
}

function requireUserId(ctx: nkruntime.Context): string {
  if (!ctx.userId) {
    throw new Error("Authentication is required.");
  }

  return ctx.userId;
}

function progressionReadRequest(userId: string): nkruntime.StorageReadRequest {
  return {
    collection: PROGRESSION_COLLECTION,
    key: PROGRESSION_KEY,
    userId: userId
  };
}

function progressionWriteRequest(
  userId: string,
  progression: PlayerProgression,
  version?: string
): nkruntime.StorageWriteRequest {
  return {
    collection: PROGRESSION_COLLECTION,
    key: PROGRESSION_KEY,
    userId: userId,
    value: progression,
    version: version,
    permissionRead: 1,
    permissionWrite: 0
  };
}

function normalizeProgression(value: {[key: string]: any} | null): PlayerProgression {
  var defaults = defaultProgression();

  if (!value) {
    return defaults;
  }

  var owned = Array.isArray(value.ownedCharacterIds)
    ? value.ownedCharacterIds.filter(function(id: any): boolean {
        return typeof id === "number" && CHARACTER_CATALOG[String(id)] !== undefined;
      })
    : defaults.ownedCharacterIds;

  if (owned.indexOf(1) < 0) {
    owned.unshift(1);
  }

  var selected = typeof value.selectedCharacterId === "number"
    ? value.selectedCharacterId
    : defaults.selectedCharacterId;

  if (owned.indexOf(selected) < 0) {
    selected = 1;
  }

  return {
    schemaVersion: PROGRESSION_SCHEMA_VERSION,
    gold: typeof value.gold === "number" ? Math.max(0, Math.floor(value.gold)) : defaults.gold,
    experience: typeof value.experience === "number" ? Math.max(0, Math.floor(value.experience)) : defaults.experience,
    level: typeof value.level === "number" ? Math.max(1, Math.floor(value.level)) : defaults.level,
    ownedCharacterIds: owned,
    selectedCharacterId: selected
  };
}

function readOrCreateProgression(
  nk: nkruntime.Nakama,
  userId: string
): PlayerProgression {
  var request = progressionReadRequest(userId);
  var objects = nk.storageRead([request]);

  if (objects.length > 0) {
    return normalizeProgression(objects[0].value);
  }

  var progression = defaultProgression();
  nk.storageWrite([progressionWriteRequest(userId, progression, "*")]);
  return progression;
}

function parseCharacterRequest(payload: string): CharacterRequest {
  var parsed: any;

  try {
    parsed = JSON.parse(payload || "{}");
  } catch (_) {
    throw new Error("Request payload must be valid JSON.");
  }

  if (!parsed || typeof parsed.characterId !== "number" || Math.floor(parsed.characterId) !== parsed.characterId) {
    throw new Error("characterId must be an integer.");
  }

  if (!CHARACTER_CATALOG[String(parsed.characterId)]) {
    throw new Error("Unknown character.");
  }

  return { characterId: parsed.characterId };
}

function mutateProgression(
  nk: nkruntime.Nakama,
  userId: string,
  mutate: (progression: PlayerProgression) => PlayerProgression
): PlayerProgression {
  var request = progressionReadRequest(userId);

  for (var attempt = 0; attempt < PROGRESSION_WRITE_RETRIES; attempt++) {
    var objects = nk.storageRead([request]);
    var current = objects.length > 0
      ? normalizeProgression(objects[0].value)
      : defaultProgression();
    var version = objects.length > 0 ? objects[0].version : "*";
    var result = mutate(current);

    try {
      nk.storageWrite([progressionWriteRequest(userId, result, version)]);
      return result;
    } catch (error) {
      if (attempt === PROGRESSION_WRITE_RETRIES - 1) {
        throw error;
      }
    }
  }

  throw new Error("Progression update failed after retries.");
}

function rpcHealthcheck(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  return JSON.stringify({
    ok: true,
    service: "bommy-nakama",
    version: SERVER_VERSION,
    userId: ctx.userId || null,
    payloadLength: payload ? payload.length : 0,
    serverTimeMs: Date.now()
  });
}

function rpcGetPlayerProgression(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  return JSON.stringify(readOrCreateProgression(nk, requireUserId(ctx)));
}

function rpcPurchaseCharacter(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  var userId = requireUserId(ctx);
  var request = parseCharacterRequest(payload);
  var catalogEntry = CHARACTER_CATALOG[String(request.characterId)];

  var progression = mutateProgression(nk, userId, function(current: PlayerProgression): PlayerProgression {
    if (current.ownedCharacterIds.indexOf(request.characterId) >= 0) {
      return current;
    }

    if (current.level < catalogEntry.requiredLevel) {
      throw new Error("Required level not reached.");
    }

    if (current.gold < catalogEntry.price) {
      throw new Error("Insufficient gold.");
    }

    current.gold -= catalogEntry.price;
    current.ownedCharacterIds.push(request.characterId);
    current.ownedCharacterIds.sort(function(a: number, b: number): number { return a - b; });
    return current;
  });

  return JSON.stringify(progression);
}

function rpcSelectCharacter(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  var userId = requireUserId(ctx);
  var request = parseCharacterRequest(payload);

  var progression = mutateProgression(nk, userId, function(current: PlayerProgression): PlayerProgression {
    if (current.ownedCharacterIds.indexOf(request.characterId) < 0) {
      throw new Error("Character is not owned.");
    }

    current.selectedCharacterId = request.characterId;
    return current;
  });

  return JSON.stringify(progression);
}

function InitModule(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  initializer: nkruntime.Initializer
): void {
  initializer.registerRpc("healthcheck", rpcHealthcheck);
  initializer.registerRpc("get_player_progression", rpcGetPlayerProgression);
  initializer.registerRpc("purchase_character", rpcPurchaseCharacter);
  initializer.registerRpc("select_character", rpcSelectCharacter);
  logger.info("Bommy Nakama TypeScript runtime loaded.");
}

!InitModule && InitModule.bind(null);
