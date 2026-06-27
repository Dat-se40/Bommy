var SERVER_VERSION = "0.2.0";
var PROGRESSION_COLLECTION = "player_progression";
var PROGRESSION_KEY = "state";
var PROGRESSION_SCHEMA_VERSION = 2;
var PROGRESSION_WRITE_RETRIES = 3;
var MATCH_SERVER_COLLECTION = "match_servers";
var MATCH_SERVER_OWNER = "00000000-0000-0000-0000-000000000000";
var MATCH_SERVER_SECRET = "dev-local-secret";
var MATCH_SERVER_STALE_MS = 15000;
var MATCH_SERVER_REQUEST_STALE_MS = 120000;
var MATCH_SERVER_PROVIDER_ENV = "MATCH_SERVER_PROVIDER";
var LOCALDEV_ORCHESTRATION_ENV = "BOMMY_LOCALDEV_ORCHESTRATION";
var RANDOM_QUEUE_REQUIRED_PLAYERS_ENV = "BOMMY_RANDOM_QUEUE_REQUIRED_PLAYERS";
var EDGEGAP_API_BASE = "https://api.edgegap.com";
var EDGEGAP_V2_API_BASE = "https://api.edgegap.com/v2";
var EDGEGAP_DEFAULT_APP_NAME = "bommy-server";
var EDGEGAP_DEFAULT_GAME_PORT_NAME = "gameport";

interface CharacterCatalogEntry {
	characterId: number;
	price: number;
}

interface PlayerProgression {
	schemaVersion: number;
	coins: number;
	// Legacy alias returned for older Unity clients and local data sheets.
	gold: number;
	trophies: number;
	experience: number;
	level: number;
	ownedCharacterIds: number[];
	selectedCharacterId: number;
	matchStats: PlayerMatchStats;
}

interface PlayerMatchStats {
	matchesPlayed: number;
	wins: number;
	kills: number;
	deaths: number;
}

interface CharacterRequest {
	characterId: number;
}

interface MatchRewardRequest {
	coinsDelta: number;
	trophiesDelta: number;
	experienceDelta: number;
	matchesPlayedDelta: number;
	winsDelta: number;
	killsDelta: number;
	deathsDelta: number;
}

type LobbyStatus = "Open" | "Starting" | "InMatch" | "Closed";
type MatchServerProvider = "LocalDev" | "EdgegapCloud";
type MatchSource = "CustomLobby" | "RandomQueue";
type MatchServerStatusName =
	| "Requested"
	| "Launching"
	| "Ready"
	| "InMatch"
	| "Settling"
	| "Resetting"
	| "Available"
	| "Failed"
	| "Released";
type RandomQueueTicketStatus =
	| "Searching"
	| "MatchFound"
	| "Accepted"
	| "Cancelled"
	| "Expired";

interface LobbyMember {
	userId: string;
	username: string;
	displayName: string;
}

interface LobbyRoomDto {
	roomId: string;
	roomName: string;
	mapId: number;
	mapName: string;
	currentPlayers: number;
	maxPlayers: number;
	pingMs: number;
	region: string;
	isPrivate: boolean;
	hostPlayerId: string;
	matchId: string;
	status: LobbyStatus;
	allocationId: string;
	serverStatus: string;
}

interface ListRoomsRequest {
	page: number;
	pageSize: number;
	region: string;
}

interface ListRoomsResponse {
	rooms: LobbyRoomDto[];
	totalCount: number;
	page: number;
	pageSize: number;
}

interface CreateLobbyRequest {
	roomName: string;
	mapId: number;
	mapName: string;
	maxPlayers: number;
	preferredRoomId: string;
}

interface JoinLobbyRequest {
	roomId: string;
	matchId: string;
	roomCode: string;
	password: string;
}

interface StartLobbyRequest {
	roomId: string;
	matchId: string;
}

interface MatchServerPlayerDto {
	userId: string;
	username: string;
	displayName: string;
	selectedCharacterId: number;
}

interface MatchServerAllocationRecord {
	allocationId: string;
	matchId: string;
	source: MatchSource;
	provider: MatchServerProvider;
	status: MatchServerStatusName;
	deploymentId: string;
	roomId: string;
	mapId: number;
	mapName: string;
	region: string;
	maxPlayers: number;
	players: MatchServerPlayerDto[];
	allowedUserIds: { [userId: string]: boolean };
	serverId: string;
	host: string;
	port: number;
	protocol: string;
	connectionToken: string;
	errorMessage: string;
	createdAtMs: number;
	updatedAtMs: number;
	lastHeartbeatMs: number;
}

interface MatchServerRecord {
	serverId: string;
	provider: MatchServerProvider;
	status: MatchServerStatusName;
	deploymentId: string;
	host: string;
	port: number;
	protocol: string;
	currentAllocationId: string;
	currentMatchId: string;
	createdAtMs: number;
	updatedAtMs: number;
	lastHeartbeatMs: number;
}

interface MatchServerAllocationResponse {
	success: boolean;
	errorMessage: string;
	allocationId: string;
	matchId: string;
	source: string;
	provider: string;
	status: string;
}

interface MatchServerStatusResponse {
	success: boolean;
	errorMessage: string;
	allocationId: string;
	matchId: string;
	status: string;
	host: string;
	port: number;
	protocol: string;
	connectionToken: string;
}

interface MatchServerRegisterRequest {
	serverId: string;
	host: string;
	port: number;
	protocol: string;
	serverSecret: string;
}

interface MatchServerRequest {
	matchId: string;
	allocationId: string;
	source: MatchSource;
	roomId: string;
	mapId: number;
	mapName: string;
	region: string;
	maxPlayers: number;
	serverId: string;
	host: string;
	port: number;
	protocol: string;
	status: string;
	serverSecret: string;
	reason: string;
}

interface MatchSettlementPlayerRequest {
	userId: string;
	placement: number;
	kills: number;
	deaths: number;
	disconnected: boolean;
}

interface MatchSettlementRequest {
	serverId: string;
	allocationId: string;
	matchId: string;
	serverSecret: string;
	results: MatchSettlementPlayerRequest[];
}

interface MatchSettlementReward {
	userId: string;
	placement: number;
	kills: number;
	deaths: number;
	coinsDelta: number;
	trophiesDelta: number;
	experienceDelta: number;
	winsDelta: number;
	matchesPlayedDelta: number;
}

interface MatchSettlementRecord {
	settlementId: string;
	allocationId: string;
	matchId: string;
	serverId: string;
	status: string;
	rewards: MatchSettlementReward[];
	createdAtMs: number;
}

interface MatchSettlementResponse {
	success: boolean;
	errorMessage: string;
	settlementId: string;
	matchId: string;
	allocationId: string;
	status: string;
	rewards: MatchSettlementReward[];
}

interface EdgegapConfig {
	enabled: boolean;
	apiToken: string;
	appName: string;
	versionName: string;
	defaultRegion: string;
	internalGamePort: number;
	protocol: string;
	portName: string;
	nakamaScheme: string;
	nakamaHost: string;
	nakamaPort: number;
	nakamaServerKey: string;
	nakamaHttpKey: string;
	serverSecret: string;
}

interface MatchLaunchConfigResponse {
	success: boolean;
	errorMessage: string;
	allocationId: string;
	matchId: string;
	source: string;
	roomId: string;
	mapId: number;
	mapName: string;
	region: string;
	maxPlayers: number;
	players: MatchServerPlayerDto[];
	nakamaHost: string;
	nakamaPort: number;
	purrnetPort: number;
}

interface LobbyActionRequest {
	action: string;
	userId: string;
	roomId?: string;
	password?: string;
}

interface LobbySignalResponse {
	success: boolean;
	errorMessage: string;
	room: LobbyRoomDto | null;
	members?: LobbyMember[];
}

interface CustomLobbyState {
	roomId: string;
	roomName: string;
	mapId: number;
	mapName: string;
	maxPlayers: number;
	isPrivate: boolean;
	hostUserId: string;
	region: string;
	status: LobbyStatus;
	members: { [userId: string]: LobbyMember };
	reservedUserIds: { [userId: string]: boolean };
}

interface RandomQueueRequest {
	mapId: number;
	mapName: string;
	region: string;
	maxPlayers: number;
	username: string;
	displayName: string;
}

interface RandomQueueActionRequest extends RandomQueueRequest {
	action: string;
	userId: string;
	username: string;
	displayName: string;
	ticketId: string;
}

interface RandomQueuePlayerDto {
	userId: string;
	username: string;
	displayName: string;
}

interface RandomMatchDto {
	matchId: string;
	roomId: string;
	status: string;
	players: RandomQueuePlayerDto[];
	allocationId: string;
	serverStatus: string;
}

interface RandomQueueStatus {
	success: boolean;
	errorMessage: string;
	ticketId: string;
	status: string;
	matchId: string;
	playerCount: number;
	maxPlayers: number;
	acceptedCount: number;
	allocationId: string;
	serverStatus: string;
	match: RandomMatchDto | null;
}

interface RandomQueueTicket {
	ticketId: string;
	userId: string;
	username: string;
	displayName: string;
	status: RandomQueueTicketStatus;
	matchId: string;
	mapId: number;
	mapName: string;
	region: string;
	maxPlayers: number;
	createdAtMs: number;
	accepted: boolean;
}

interface RandomQueueMatchRecord {
	matchId: string;
	roomId: string;
	status: string;
	players: RandomQueuePlayerDto[];
	acceptedUserIds: { [userId: string]: boolean };
	mapId: number;
	mapName: string;
	region: string;
	maxPlayers: number;
	allocationId: string;
	serverStatus: string;
	createdAtMs: number;
}

interface RandomQueueState {
	queue: string[];
	ticketsById: { [ticketId: string]: RandomQueueTicket };
	ticketIdByUserId: { [userId: string]: string };
	matchesById: { [matchId: string]: RandomQueueMatchRecord };
	sequence: number;
}

var CHARACTER_CATALOG: { [characterId: string]: CharacterCatalogEntry } = {
	"1": { characterId: 1, price: 0 },
	"2": { characterId: 2, price: 5 },
	"3": { characterId: 3, price: 10 },
	"4": { characterId: 4, price: 15 },
	"5": { characterId: 5, price: 20 },
	"6": { characterId: 6, price: 25 },
	"7": { characterId: 7, price: 30 },
	"8": { characterId: 8, price: 40 },
	"9": { characterId: 9, price: 50 },
	"10": { characterId: 10, price: 60 },
	"11": { characterId: 11, price: 70 },
	"12": { characterId: 12, price: 75 },
	"13": { characterId: 13, price: 80 },
};

function defaultProgression(): PlayerProgression {
	return {
		schemaVersion: PROGRESSION_SCHEMA_VERSION,
		coins: 850,
		gold: 850,
		trophies: 0,
		experience: 0,
		level: 1,
		ownedCharacterIds: [1],
		selectedCharacterId: 1,
		matchStats: defaultMatchStats(),
	};
}

function defaultMatchStats(): PlayerMatchStats {
	return {
		matchesPlayed: 0,
		wins: 0,
		kills: 0,
		deaths: 0,
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
		userId: userId,
	};
}

function progressionWriteRequest(
	userId: string,
	progression: PlayerProgression,
	version?: string,
): nkruntime.StorageWriteRequest {
	return {
		collection: PROGRESSION_COLLECTION,
		key: PROGRESSION_KEY,
		userId: userId,
		value: progression,
		version: version,
		permissionRead: 1,
		permissionWrite: 0,
	};
}

function normalizeProgression(
	value: { [key: string]: any } | null,
): PlayerProgression {
	var defaults = defaultProgression();

	if (!value) {
		return defaults;
	}

	var coins =
		typeof value.coins === "number"
			? Math.max(0, Math.floor(value.coins))
			: typeof value.gold === "number"
				? Math.max(0, Math.floor(value.gold))
				: defaults.coins;

	var rawStats = value.matchStats || {};
	var matchStats: PlayerMatchStats = {
		matchesPlayed:
			typeof rawStats.matchesPlayed === "number"
				? Math.max(0, Math.floor(rawStats.matchesPlayed))
				: defaults.matchStats.matchesPlayed,
		wins:
			typeof rawStats.wins === "number"
				? Math.max(0, Math.floor(rawStats.wins))
				: defaults.matchStats.wins,
		kills:
			typeof rawStats.kills === "number"
				? Math.max(0, Math.floor(rawStats.kills))
				: defaults.matchStats.kills,
		deaths:
			typeof rawStats.deaths === "number"
				? Math.max(0, Math.floor(rawStats.deaths))
				: defaults.matchStats.deaths,
	};

	var owned = Array.isArray(value.ownedCharacterIds)
		? value.ownedCharacterIds.filter(function (id: any): boolean {
				return (
					typeof id === "number" && CHARACTER_CATALOG[String(id)] !== undefined
				);
			})
		: defaults.ownedCharacterIds;

	if (owned.indexOf(1) < 0) {
		owned.unshift(1);
	}

	var selected =
		typeof value.selectedCharacterId === "number"
			? value.selectedCharacterId
			: defaults.selectedCharacterId;

	if (owned.indexOf(selected) < 0) {
		selected = 1;
	}

	return {
		schemaVersion: PROGRESSION_SCHEMA_VERSION,
		coins: coins,
		gold: coins,
		trophies:
			typeof value.trophies === "number"
				? Math.max(0, Math.floor(value.trophies))
				: defaults.trophies,
		experience:
			typeof value.experience === "number"
				? Math.max(0, Math.floor(value.experience))
				: defaults.experience,
		level:
			typeof value.level === "number"
				? Math.max(1, Math.floor(value.level))
				: defaults.level,
		ownedCharacterIds: owned,
		selectedCharacterId: selected,
		matchStats: matchStats,
	};
}

function readOrCreateProgression(
	nk: nkruntime.Nakama,
	userId: string,
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

	if (
		!parsed ||
		typeof parsed.characterId !== "number" ||
		Math.floor(parsed.characterId) !== parsed.characterId
	) {
		throw new Error("characterId must be an integer.");
	}

	if (!CHARACTER_CATALOG[String(parsed.characterId)]) {
		throw new Error("Unknown character.");
	}

	return { characterId: parsed.characterId };
}

function optionalInteger(value: any, fallback: number): number {
	if (typeof value !== "number" || Math.floor(value) !== value) {
		return fallback;
	}

	return value;
}

function parseMatchRewardRequest(payload: string): MatchRewardRequest {
	var parsed: any;

	try {
		parsed = JSON.parse(payload || "{}");
	} catch (_) {
		throw new Error("Request payload must be valid JSON.");
	}

	parsed = parsed || {};

	return {
		coinsDelta: optionalInteger(
			parsed.coinsDelta,
			optionalInteger(parsed.goldDelta, 0),
		),
		trophiesDelta: optionalInteger(parsed.trophiesDelta, 0),
		experienceDelta: optionalInteger(parsed.experienceDelta, 0),
		matchesPlayedDelta: Math.max(
			0,
			optionalInteger(parsed.matchesPlayedDelta, 0),
		),
		winsDelta: Math.max(0, optionalInteger(parsed.winsDelta, 0)),
		killsDelta: Math.max(0, optionalInteger(parsed.killsDelta, 0)),
		deathsDelta: Math.max(0, optionalInteger(parsed.deathsDelta, 0)),
	};
}

function parseJsonPayload(payload: string): any {
	try {
		return JSON.parse(payload || "{}") || {};
	} catch (_) {
		throw new Error("Request payload must be valid JSON.");
	}
}

function optionalString(value: any, fallback: string): string {
	if (typeof value !== "string") {
		return fallback;
	}

	var trimmed = value.trim();
	return trimmed.length > 0 ? trimmed : fallback;
}

function clampInteger(value: any, fallback: number, min: number, max: number): number {
	var integer = optionalInteger(value, fallback);
	return Math.max(min, Math.min(max, integer));
}

function readEnvString(ctx: nkruntime.Context, key: string, fallback: string): string {
	if (ctx.env == null) {
		return fallback;
	}

	return optionalString(ctx.env[key], fallback);
}

function readEnvInteger(ctx: nkruntime.Context, key: string, fallback: number): number {
	if (ctx.env == null) {
		return fallback;
	}

	var value = ctx.env[key];
	if (typeof value !== "string") {
		return fallback;
	}

	var parsed = parseInt(value, 10);
	return isNaN(parsed) ? fallback : parsed;
}

function envFlag(ctx: nkruntime.Context, key: string): boolean {
	var value = readEnvString(ctx, key, "").toLowerCase();
	return value === "1" || value === "true" || value === "yes";
}

function normalizeProvider(value: string): MatchServerProvider {
	var provider = optionalString(value, "EdgegapCloud");
	if (provider === "LocalPool" || provider === "LocalDev") {
		return "LocalDev";
	}

	return "EdgegapCloud";
}

function configuredMatchServerProvider(ctx: nkruntime.Context): MatchServerProvider {
	return normalizeProvider(readEnvString(ctx, MATCH_SERVER_PROVIDER_ENV, "EdgegapCloud"));
}

function isRailwayRuntime(ctx: nkruntime.Context): boolean {
	return !!(
		readEnvString(ctx, "RAILWAY_ENVIRONMENT", "") ||
		readEnvString(ctx, "RAILWAY_PROJECT_ID", "") ||
		readEnvString(ctx, "RAILWAY_SERVICE_ID", "") ||
		readEnvString(ctx, "RAILWAY_DEPLOYMENT_ID", "")
	);
}

function localDevOrchestrationAllowed(ctx: nkruntime.Context): boolean {
	return configuredMatchServerProvider(ctx) === "LocalDev" && envFlag(ctx, LOCALDEV_ORCHESTRATION_ENV) && !isRailwayRuntime(ctx);
}

function randomQueueRequiredPlayers(ctx: nkruntime.Context, maxPlayers: number): number {
	var configured = readEnvInteger(ctx, RANDOM_QUEUE_REQUIRED_PLAYERS_ENV, maxPlayers);
	return clampInteger(configured, maxPlayers, 1, maxPlayers);
}

function readEdgegapConfig(ctx: nkruntime.Context): EdgegapConfig {
	var provider = configuredMatchServerProvider(ctx);
	var apiToken = readEnvString(ctx, "EDGEGAP_API_TOKEN", "");
	var versionName = readEnvString(ctx, "EDGEGAP_VERSION_NAME", "");

	return {
		enabled:
			provider === "EdgegapCloud" ||
			envFlag(ctx, "EDGEGAP_ENABLED"),
		apiToken: apiToken,
		appName: readEnvString(ctx, "EDGEGAP_APP_NAME", EDGEGAP_DEFAULT_APP_NAME),
		versionName: versionName,
		defaultRegion: readEnvString(ctx, "EDGEGAP_DEFAULT_REGION", "Local"),
		internalGamePort: Math.max(1, readEnvInteger(ctx, "EDGEGAP_INTERNAL_GAME_PORT", 5000)),
		protocol: readEnvString(ctx, "EDGEGAP_PROTOCOL", "UDP").toUpperCase(),
		portName: readEnvString(ctx, "EDGEGAP_PORT_NAME", EDGEGAP_DEFAULT_GAME_PORT_NAME),
		nakamaScheme: readEnvString(ctx, "BOMMY_PUBLIC_NAKAMA_SCHEME", readEnvString(ctx, "BOMMY_NAKAMA_SCHEME", provider === "EdgegapCloud" ? "https" : "http")).toLowerCase(),
		nakamaHost: readEnvString(ctx, "BOMMY_PUBLIC_NAKAMA_HOST", readEnvString(ctx, "BOMMY_NAKAMA_HOST", "127.0.0.1")),
		nakamaPort: Math.max(1, readEnvInteger(ctx, "BOMMY_NAKAMA_PORT", 7350)),
		nakamaServerKey: readEnvString(ctx, "BOMMY_NAKAMA_SERVER_KEY", "defaultkey"),
		nakamaHttpKey: readEnvString(ctx, "BOMMY_NAKAMA_HTTP_KEY", "defaulthttpkey"),
		serverSecret: readEnvString(ctx, "BOMMY_SERVER_SECRET", MATCH_SERVER_SECRET),
	};
}

function edgegapHeaders(config: EdgegapConfig): { [header: string]: string } {
	return {
		Authorization: config.apiToken,
		Accept: "application/json",
		"Content-Type": "application/json",
	};
}

function parseHttpJson(response: nkruntime.HttpResponse): any {
	if (!response.body) {
		return {};
	}

	try {
		return JSON.parse(response.body);
	} catch (_) {
		return {};
	}
}

function edgegapErrorMessage(response: nkruntime.HttpResponse, fallback: string): string {
	var body = parseHttpJson(response);
	return optionalString(body.message, fallback + " HTTP " + response.code);
}

function edgegapSafeTag(value: string): string {
	var result = "";
	for (var i = 0; i < value.length && result.length < 40; i++) {
		var ch = value.charAt(i);
		if ((ch >= "a" && ch <= "z") || (ch >= "A" && ch <= "Z") || (ch >= "0" && ch <= "9") || ch === "-" || ch === "_") {
			result += ch;
		}
	}

	return result || "bommy-match";
}

function matchServerReadRequest(key: string): nkruntime.StorageReadRequest {
	return {
		collection: MATCH_SERVER_COLLECTION,
		key: key,
		userId: MATCH_SERVER_OWNER,
	};
}

function matchServerWriteRequest(
	key: string,
	value: { [key: string]: any },
	version?: string,
): nkruntime.StorageWriteRequest {
	var request: nkruntime.StorageWriteRequest = {
		collection: MATCH_SERVER_COLLECTION,
		key: key,
		userId: MATCH_SERVER_OWNER,
		value: value,
		permissionRead: 0,
		permissionWrite: 0,
	};

	if (version) {
		request.version = version;
	}

	return request;
}

function allocationKey(matchId: string): string {
	return "allocation_" + matchId;
}

function serverKey(serverId: string): string {
	return "server_" + serverId;
}

function settlementKey(matchId: string): string {
	return "settlement_" + matchId;
}

function readStorageValue(
	nk: nkruntime.Nakama,
	key: string,
): { value: { [key: string]: any } | null; version: string } {
	var objects = nk.storageRead([matchServerReadRequest(key)]);

	if (objects.length <= 0) {
		return { value: null, version: "*" };
	}

	return {
		value: objects[0].value,
		version: objects[0].version,
	};
}

function writeMatchServerValue(
	nk: nkruntime.Nakama,
	key: string,
	value: { [key: string]: any },
	version?: string,
): void {
	nk.storageWrite([matchServerWriteRequest(key, value, version)]);
}

function normalizeMatchServerPlayer(value: any): MatchServerPlayerDto {
	value = value || {};
	return {
		userId: optionalString(value.userId, ""),
		username: optionalString(value.username, optionalString(value.userId, "Player")),
		displayName: optionalString(value.displayName, optionalString(value.username, "Player")),
		selectedCharacterId: Math.max(1, optionalInteger(value.selectedCharacterId, 1)),
	};
}

function normalizeAllocation(value: { [key: string]: any } | null): MatchServerAllocationRecord | null {
	if (value == null) {
		return null;
	}

	var players: MatchServerPlayerDto[] = [];
	if (Array.isArray(value.players)) {
		for (var i = 0; i < value.players.length; i++) {
			players.push(normalizeMatchServerPlayer(value.players[i]));
		}
	}

	var allowedUserIds: { [userId: string]: boolean } = {};
	var rawAllowed = value.allowedUserIds || {};
	for (var userId in rawAllowed) {
		if (rawAllowed.hasOwnProperty(userId) && rawAllowed[userId]) {
			allowedUserIds[userId] = true;
		}
	}

	for (var p = 0; p < players.length; p++) {
		if (players[p].userId) {
			allowedUserIds[players[p].userId] = true;
		}
	}

	return {
		allocationId: optionalString(value.allocationId, ""),
		matchId: optionalString(value.matchId, ""),
		source: optionalString(value.source, "RandomQueue") as MatchSource,
		provider: normalizeProvider(optionalString(value.provider, "EdgegapCloud")),
		status: optionalString(value.status, "Requested") as MatchServerStatusName,
		deploymentId: optionalString(value.deploymentId, ""),
		roomId: optionalString(value.roomId, ""),
		mapId: Math.max(0, optionalInteger(value.mapId, 0)),
		mapName: optionalString(value.mapName, "Classic Garden"),
		region: optionalString(value.region, "Local"),
		maxPlayers: Math.max(1, optionalInteger(value.maxPlayers, 4)),
		players: players,
		allowedUserIds: allowedUserIds,
		serverId: optionalString(value.serverId, ""),
		host: optionalString(value.host, ""),
		port: Math.max(0, optionalInteger(value.port, 0)),
		protocol: optionalString(value.protocol, "UDP"),
		connectionToken: optionalString(value.connectionToken, ""),
		errorMessage: optionalString(value.errorMessage, ""),
		createdAtMs: Math.max(0, optionalInteger(value.createdAtMs, Date.now())),
		updatedAtMs: Math.max(0, optionalInteger(value.updatedAtMs, Date.now())),
		lastHeartbeatMs: Math.max(0, optionalInteger(value.lastHeartbeatMs, 0)),
	};
}

function normalizeServer(value: { [key: string]: any } | null): MatchServerRecord | null {
	if (value == null) {
		return null;
	}

	return {
		serverId: optionalString(value.serverId, ""),
		provider: normalizeProvider(optionalString(value.provider, "LocalDev")),
		status: optionalString(value.status, "Available") as MatchServerStatusName,
		deploymentId: optionalString(value.deploymentId, ""),
		host: optionalString(value.host, "127.0.0.1"),
		port: Math.max(0, optionalInteger(value.port, 5000)),
		protocol: optionalString(value.protocol, "UDP"),
		currentAllocationId: optionalString(value.currentAllocationId, ""),
		currentMatchId: optionalString(value.currentMatchId, ""),
		createdAtMs: Math.max(0, optionalInteger(value.createdAtMs, Date.now())),
		updatedAtMs: Math.max(0, optionalInteger(value.updatedAtMs, Date.now())),
		lastHeartbeatMs: Math.max(0, optionalInteger(value.lastHeartbeatMs, 0)),
	};
}

function readAllocation(nk: nkruntime.Nakama, matchId: string): MatchServerAllocationRecord | null {
	var result = readStorageValue(nk, allocationKey(matchId));
	return normalizeAllocation(result.value);
}

function writeAllocation(nk: nkruntime.Nakama, allocation: MatchServerAllocationRecord): void {
	allocation.updatedAtMs = Date.now();
	writeMatchServerValue(nk, allocationKey(allocation.matchId), allocation as any);
}

function readServer(nk: nkruntime.Nakama, serverId: string): MatchServerRecord | null {
	var result = readStorageValue(nk, serverKey(serverId));
	return normalizeServer(result.value);
}

function writeServer(nk: nkruntime.Nakama, server: MatchServerRecord): void {
	server.updatedAtMs = Date.now();
	writeMatchServerValue(nk, serverKey(server.serverId), server as any);
}

function normalizeSettlementReward(value: any): MatchSettlementReward {
	value = value || {};
	return {
		userId: optionalString(value.userId, ""),
		placement: Math.max(1, optionalInteger(value.placement, 4)),
		kills: Math.max(0, optionalInteger(value.kills, 0)),
		deaths: Math.max(0, optionalInteger(value.deaths, 0)),
		coinsDelta: optionalInteger(value.coinsDelta, 0),
		trophiesDelta: optionalInteger(value.trophiesDelta, 0),
		experienceDelta: optionalInteger(value.experienceDelta, 0),
		winsDelta: Math.max(0, optionalInteger(value.winsDelta, 0)),
		matchesPlayedDelta: Math.max(0, optionalInteger(value.matchesPlayedDelta, 0)),
	};
}

function normalizeSettlement(value: { [key: string]: any } | null): MatchSettlementRecord | null {
	if (value == null) {
		return null;
	}

	var rewards: MatchSettlementReward[] = [];
	if (Array.isArray(value.rewards)) {
		for (var i = 0; i < value.rewards.length; i++) {
			rewards.push(normalizeSettlementReward(value.rewards[i]));
		}
	}

	return {
		settlementId: optionalString(value.settlementId, ""),
		allocationId: optionalString(value.allocationId, ""),
		matchId: optionalString(value.matchId, ""),
		serverId: optionalString(value.serverId, ""),
		status: optionalString(value.status, "Settled"),
		rewards: rewards,
		createdAtMs: Math.max(0, optionalInteger(value.createdAtMs, Date.now())),
	};
}

function readSettlement(nk: nkruntime.Nakama, matchId: string): MatchSettlementRecord | null {
	var result = readStorageValue(nk, settlementKey(matchId));
	return normalizeSettlement(result.value);
}

function writeSettlement(nk: nkruntime.Nakama, settlement: MatchSettlementRecord): void {
	writeMatchServerValue(nk, settlementKey(settlement.matchId), settlement as any, "*");
}

function listGlobalStorage(nk: nkruntime.Nakama, collection: string, limit: number): nkruntime.StorageObject[] {
	var result = nk.storageList(MATCH_SERVER_OWNER, collection, limit);
	return result.objects || [];
}

function listServers(nk: nkruntime.Nakama): MatchServerRecord[] {
	var objects = listGlobalStorage(nk, MATCH_SERVER_COLLECTION, 100);
	var servers: MatchServerRecord[] = [];

	for (var i = 0; i < objects.length; i++) {
		if (objects[i].key.indexOf("server_") !== 0) {
			continue;
		}

		var server = normalizeServer(objects[i].value);
		if (server != null && server.serverId) {
			servers.push(server);
		}
	}

	return servers;
}

function listAllocations(nk: nkruntime.Nakama): MatchServerAllocationRecord[] {
	var objects = listGlobalStorage(nk, MATCH_SERVER_COLLECTION, 100);
	var allocations: MatchServerAllocationRecord[] = [];

	for (var i = 0; i < objects.length; i++) {
		if (objects[i].key.indexOf("allocation_") !== 0) {
			continue;
		}

		var allocation = normalizeAllocation(objects[i].value);
		if (allocation != null && allocation.matchId) {
			allocations.push(allocation);
		}
	}

	return allocations;
}

function validateServerSecret(ctx: nkruntime.Context, secret: string): void {
	if (optionalString(secret, "") !== readEnvString(ctx, "BOMMY_SERVER_SECRET", MATCH_SERVER_SECRET)) {
		throw new Error("Server secret is invalid.");
	}
}

function allocationResponse(
	allocation: MatchServerAllocationRecord | null,
	success?: boolean,
	errorMessage?: string,
): MatchServerAllocationResponse {
	return {
		success: success === true || (success !== false && allocation != null),
		errorMessage: errorMessage || (allocation != null ? allocation.errorMessage : "Allocation not found."),
		allocationId: allocation != null ? allocation.allocationId : "",
		matchId: allocation != null ? allocation.matchId : "",
		source: allocation != null ? allocation.source : "",
		provider: allocation != null ? allocation.provider : "",
		status: allocation != null ? allocation.status : "",
	};
}

function statusResponse(
	allocation: MatchServerAllocationRecord | null,
	success?: boolean,
	errorMessage?: string,
): MatchServerStatusResponse {
	return {
		success: success === true || (success !== false && allocation != null),
		errorMessage: errorMessage || (allocation != null ? allocation.errorMessage : "Allocation not found."),
		allocationId: allocation != null ? allocation.allocationId : "",
		matchId: allocation != null ? allocation.matchId : "",
		status: allocation != null ? allocation.status : "",
		host: allocation != null ? allocation.host : "",
		port: allocation != null ? allocation.port : 0,
		protocol: allocation != null ? allocation.protocol : "UDP",
		connectionToken: allocation != null ? allocation.connectionToken : "",
	};
}

function selectedCharacterForUser(nk: nkruntime.Nakama, userId: string): number {
	if (!userId) {
		return 1;
	}

	try {
		return readOrCreateProgression(nk, userId).selectedCharacterId;
	} catch (_) {
		return 1;
	}
}

function matchServerPlayersFromRandom(
	nk: nkruntime.Nakama,
	players: RandomQueuePlayerDto[],
): MatchServerPlayerDto[] {
	var result: MatchServerPlayerDto[] = [];

	for (var i = 0; i < players.length; i++) {
		result.push({
			userId: players[i].userId,
			username: players[i].username,
			displayName: players[i].displayName,
			selectedCharacterId: selectedCharacterForUser(nk, players[i].userId),
		});
	}

	return result;
}

function matchServerPlayersFromLobby(
	nk: nkruntime.Nakama,
	members: LobbyMember[] | undefined,
	fallbackUserId: string,
	fallbackUsername: string,
): MatchServerPlayerDto[] {
	var result: MatchServerPlayerDto[] = [];

	if (members != null) {
		for (var i = 0; i < members.length; i++) {
			result.push({
				userId: members[i].userId,
				username: members[i].username,
				displayName: members[i].displayName,
				selectedCharacterId: selectedCharacterForUser(nk, members[i].userId),
			});
		}
	}

	if (result.length === 0 && fallbackUserId) {
		result.push({
			userId: fallbackUserId,
			username: optionalString(fallbackUsername, fallbackUserId),
			displayName: optionalString(fallbackUsername, fallbackUserId),
			selectedCharacterId: selectedCharacterForUser(nk, fallbackUserId),
		});
	}

	return result;
}

function createConnectionToken(matchId: string): string {
	return "conn-" + matchId + "-" + Math.floor(Math.random() * 1000000000);
}

function isServerStale(server: MatchServerRecord, now: number): boolean {
	if (server.status === "Available" || server.status === "Launching" || server.status === "Ready" || server.status === "InMatch") {
		return server.lastHeartbeatMs > 0 && now - server.lastHeartbeatMs > MATCH_SERVER_STALE_MS;
	}

	return false;
}

function markAllocationFailed(
	nk: nkruntime.Nakama,
	allocation: MatchServerAllocationRecord,
	message: string,
): MatchServerAllocationRecord {
	allocation.status = "Failed";
	allocation.errorMessage = message;
	writeAllocation(nk, allocation);
	return allocation;
}

function edgegapServerId(allocation: MatchServerAllocationRecord): string {
	return "edgegap-" + allocation.allocationId;
}

function edgegapDeploymentHost(status: any): string {
	return optionalString(status.fqdn, optionalString(status.public_ip, ""));
}

function edgegapDeploymentPort(status: any, config: EdgegapConfig): number {
	var ports = status != null ? status.ports : null;
	if (ports == null) {
		return 0;
	}

	var namedPort = ports[config.portName];
	if (namedPort != null) {
		return Math.max(0, optionalInteger(namedPort.external, 0));
	}

	for (var key in ports) {
		if (!ports.hasOwnProperty(key)) {
			continue;
		}

		var port = ports[key];
		if (
			port != null &&
			optionalString(port.protocol, "").toUpperCase() === config.protocol &&
			optionalInteger(port.internal, 0) === config.internalGamePort
		) {
			return Math.max(0, optionalInteger(port.external, 0));
		}
	}

	return 0;
}

function createEdgegapDeployment(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	logger: nkruntime.Logger,
	allocation: MatchServerAllocationRecord,
): MatchServerAllocationRecord {
	var config = readEdgegapConfig(ctx);
	if (!config.enabled) {
		logger.warn("Edgegap deployment skipped because Edgegap is disabled. matchId=%s allocationId=%s", allocation.matchId, allocation.allocationId);
		return allocation;
	}

	if (!config.apiToken || !config.versionName) {
		logger.error("Edgegap deployment config missing. matchId=%s allocationId=%s hasToken=%s hasVersion=%s", allocation.matchId, allocation.allocationId, !!config.apiToken, !!config.versionName);
		return markAllocationFailed(nk, allocation, "Edgegap is enabled but EDGEGAP_API_TOKEN or EDGEGAP_VERSION_NAME is missing.");
	}

	logger.info("Edgegap deployment requested. matchId=%s allocationId=%s app=%s version=%s port=%s/%s", allocation.matchId, allocation.allocationId, config.appName, config.versionName, config.internalGamePort, config.protocol);

	allocation.provider = "EdgegapCloud";
	allocation.status = "Launching";
	allocation.serverId = edgegapServerId(allocation);
	allocation.host = "";
	allocation.port = 0;
	allocation.protocol = config.protocol;
	allocation.errorMessage = "";
	writeAllocation(nk, allocation);

	var server: MatchServerRecord = {
		serverId: allocation.serverId,
		provider: "EdgegapCloud",
		status: "Launching",
		deploymentId: "",
		host: "",
		port: config.internalGamePort,
		protocol: config.protocol,
		currentAllocationId: allocation.allocationId,
		currentMatchId: allocation.matchId,
		createdAtMs: Date.now(),
		updatedAtMs: Date.now(),
		lastHeartbeatMs: 0,
	};
	writeServer(nk, server);

	var body = {
		application: config.appName,
		version: config.versionName,
		users: [
			{
				user_type: "geo_coordinates",
				user_data: {
					latitude: 0,
					longitude: 0,
				},
			},
		],
		tags: ["bommy", edgegapSafeTag(allocation.matchId), edgegapSafeTag(allocation.allocationId)],
		environment_variables: [
			{ key: "BOMMY_DEDICATED_SERVER", value: "1", is_hidden: false },
			{ key: "BOMMY_PROVIDER", value: "EdgegapCloud", is_hidden: false },
			{ key: "BOMMY_SERVER_ID", value: allocation.serverId, is_hidden: false },
			{ key: "BOMMY_ALLOCATION_ID", value: allocation.allocationId, is_hidden: false },
			{ key: "BOMMY_MATCH_ID", value: allocation.matchId, is_hidden: false },
			{ key: "BOMMY_NAKAMA_SCHEME", value: config.nakamaScheme, is_hidden: false },
			{ key: "BOMMY_NAKAMA_HOST", value: config.nakamaHost, is_hidden: false },
			{ key: "BOMMY_NAKAMA_PORT", value: String(config.nakamaPort), is_hidden: false },
			{ key: "BOMMY_NAKAMA_SERVER_KEY", value: config.nakamaServerKey, is_hidden: true },
			{ key: "BOMMY_NAKAMA_HTTP_KEY", value: config.nakamaHttpKey, is_hidden: true },
			{ key: "BOMMY_SERVER_SECRET", value: config.serverSecret, is_hidden: true },
			{ key: "BOMMY_PURRNET_PORT", value: String(config.internalGamePort), is_hidden: false },
		],
	};

	var response = nk.httpRequest(
		EDGEGAP_V2_API_BASE + "/deployments",
		"post",
		edgegapHeaders(config),
		JSON.stringify(body),
		10000,
	);

	if (response.code !== 202) {
		logger.error("Edgegap deployment request failed. matchId=%s allocationId=%s http=%s", allocation.matchId, allocation.allocationId, response.code);
		return markAllocationFailed(nk, allocation, edgegapErrorMessage(response, "Edgegap deployment failed."));
	}

	var parsed = parseHttpJson(response);
	var deploymentId = optionalString(parsed.request_id, "");
	if (!deploymentId) {
		logger.error("Edgegap deployment response missing request_id. matchId=%s allocationId=%s", allocation.matchId, allocation.allocationId);
		return markAllocationFailed(nk, allocation, "Edgegap deployment response did not include request_id.");
	}

	allocation.deploymentId = deploymentId;
	server.deploymentId = deploymentId;
	writeAllocation(nk, allocation);
	writeServer(nk, server);
	logger.info("Edgegap deployment accepted. matchId=%s allocationId=%s deploymentId=%s", allocation.matchId, allocation.allocationId, deploymentId);
	return allocation;
}

function refreshEdgegapDeploymentStatus(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	allocation: MatchServerAllocationRecord,
): MatchServerAllocationRecord {
	if (allocation.provider !== "EdgegapCloud" || !allocation.deploymentId) {
		return allocation;
	}

	if (allocation.status !== "Launching" && allocation.status !== "Requested" && allocation.status !== "Failed") {
		return allocation;
	}

	var config = readEdgegapConfig(ctx);
	if (!config.apiToken) {
		return allocation;
	}

	var response = nk.httpRequest(
		EDGEGAP_API_BASE + "/v1/status/" + encodeURIComponent(allocation.deploymentId),
		"get",
		edgegapHeaders(config),
		"",
		5000,
	);

	if (response.code < 200 || response.code >= 300) {
		return markAllocationFailed(nk, allocation, edgegapErrorMessage(response, "Edgegap status request failed."));
	}

	var status = parseHttpJson(response);
	if (status.error === true || optionalString(status.current_status, "").toLowerCase() === "error") {
		return markAllocationFailed(nk, allocation, optionalString(status.error_detail, "Edgegap deployment entered error state."));
	}

	var host = edgegapDeploymentHost(status);
	var port = edgegapDeploymentPort(status, config);
	if (host) {
		allocation.host = host;
	}
	if (port > 0) {
		allocation.port = port;
	}
	allocation.protocol = config.protocol;

	var server = allocation.serverId ? readServer(nk, allocation.serverId) : null;
	if (server != null) {
		server.host = allocation.host;
		server.port = allocation.port > 0 ? allocation.port : config.internalGamePort;
		server.protocol = allocation.protocol;
		server.deploymentId = allocation.deploymentId;
		server.status = allocation.status;
		writeServer(nk, server);
	}

	if (status.running === true && optionalString(status.current_status, "").toLowerCase() === "ready" && allocation.host && allocation.port > 0) {
		allocation.status = "Ready";
		allocation.errorMessage = "";
	}

	writeAllocation(nk, allocation);
	return allocation;
}

function stopEdgegapDeployment(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	allocation: MatchServerAllocationRecord,
): void {
	if (allocation.provider !== "EdgegapCloud" || !allocation.deploymentId) {
		return;
	}

	var config = readEdgegapConfig(ctx);
	if (!config.apiToken) {
		return;
	}

	nk.httpRequest(
		EDGEGAP_API_BASE + "/v1/stop/" + encodeURIComponent(allocation.deploymentId),
		"delete",
		edgegapHeaders(config),
		"",
		5000,
	);
}

function findAvailableServer(nk: nkruntime.Nakama, provider: MatchServerProvider): MatchServerRecord | null {
	var now = Date.now();
	var servers = listServers(nk);

	for (var i = 0; i < servers.length; i++) {
		var server = servers[i];
		if (server.provider !== provider) {
			continue;
		}

		if (isServerStale(server, now)) {
			server.status = "Failed";
			writeServer(nk, server);
			continue;
		}

		if (server.status === "Available" && !server.currentAllocationId) {
			return server;
		}
	}

	return null;
}

function assignServerToAllocation(
	nk: nkruntime.Nakama,
	allocation: MatchServerAllocationRecord,
	server: MatchServerRecord,
): MatchServerAllocationRecord {
	allocation.status = "Launching";
	allocation.provider = server.provider;
	allocation.serverId = server.serverId;
	allocation.host = server.host;
	allocation.port = server.port;
	allocation.protocol = server.protocol;
	allocation.errorMessage = "";

	server.status = "Launching";
	server.currentAllocationId = allocation.allocationId;
	server.currentMatchId = allocation.matchId;

	writeServer(nk, server);
	writeAllocation(nk, allocation);
	return allocation;
}

function localDevOrchestrationError(ctx: nkruntime.Context): string {
	if (isRailwayRuntime(ctx)) {
		return "LocalDev orchestration is disabled on Railway. Use MATCH_SERVER_PROVIDER=EdgegapCloud.";
	}

	if (configuredMatchServerProvider(ctx) !== "LocalDev") {
		return "LocalDev orchestration is not the configured match server provider.";
	}

	if (!envFlag(ctx, LOCALDEV_ORCHESTRATION_ENV)) {
		return "LocalDev orchestration requires BOMMY_LOCALDEV_ORCHESTRATION=1.";
	}

	return "";
}

function requestLocalDevAllocation(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	logger: nkruntime.Logger,
	allocation: MatchServerAllocationRecord,
): MatchServerAllocationRecord {
	var error = localDevOrchestrationError(ctx);
	if (error) {
		logger.error("LocalDev allocation rejected. matchId=%s allocationId=%s error=%s", allocation.matchId, allocation.allocationId, error);
		return markAllocationFailed(nk, allocation, error);
	}

	var server = findAvailableServer(nk, "LocalDev");
	if (server != null) {
		logger.info("LocalDev allocation assigned. matchId=%s allocationId=%s serverId=%s public=%s:%s", allocation.matchId, allocation.allocationId, server.serverId, server.host, server.port);
		return assignServerToAllocation(nk, allocation, server);
	}

	logNoAvailableServer(logger, nk, "LocalDev", "localdev_allocation");
	writeAllocation(nk, allocation);
	logger.info("LocalDev allocation waiting for server. matchId=%s allocationId=%s source=%s players=%s", allocation.matchId, allocation.allocationId, allocation.source, allocation.players.length);
	return allocation;
}

function requestConfiguredOrchestratorAllocation(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	logger: nkruntime.Logger,
	allocation: MatchServerAllocationRecord,
): MatchServerAllocationRecord {
	var provider = configuredMatchServerProvider(ctx);
	allocation.provider = provider;
	logger.info("Match server allocation requested. matchId=%s allocationId=%s source=%s provider=%s players=%s", allocation.matchId, allocation.allocationId, allocation.source, provider, allocation.players.length);

	var server = findAvailableServer(nk, provider);
	if (server != null) {
		logger.info("Match server allocation assigned from reusable pool. matchId=%s allocationId=%s provider=%s serverId=%s public=%s:%s", allocation.matchId, allocation.allocationId, provider, server.serverId, server.host, server.port);
		return assignServerToAllocation(nk, allocation, server);
	}

	if (provider === "LocalDev") {
		return requestLocalDevAllocation(ctx, nk, logger, allocation);
	}

	logNoAvailableServer(logger, nk, provider, "edgegap_reuse_check");
	writeAllocation(nk, allocation);
	return createEdgegapDeployment(ctx, nk, logger, allocation);
}

function createOrGetAllocation(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	logger: nkruntime.Logger,
	source: MatchSource,
	matchId: string,
	roomId: string,
	mapId: number,
	mapName: string,
	region: string,
	maxPlayers: number,
	players: MatchServerPlayerDto[],
): MatchServerAllocationRecord {
	var existing = readAllocation(nk, matchId);
	if (existing != null) {
		logger.info("Match server allocation reused. matchId=%s allocationId=%s provider=%s status=%s", existing.matchId, existing.allocationId, existing.provider, existing.status);
		return existing;
	}

	var allowedUserIds: { [userId: string]: boolean } = {};
	for (var i = 0; i < players.length; i++) {
		if (players[i].userId) {
			allowedUserIds[players[i].userId] = true;
		}
	}

	var now = Date.now();
	var allocation: MatchServerAllocationRecord = {
		allocationId: "alloc-" + matchId + "-" + Math.floor(Math.random() * 1000000),
		matchId: matchId,
		source: source,
		provider: configuredMatchServerProvider(ctx),
		status: "Requested",
		deploymentId: "",
		roomId: roomId,
		mapId: Math.max(0, mapId),
		mapName: optionalString(mapName, Math.max(0, mapId) === 0 ? "Random" : "Classic Garden"),
		region: optionalString(region, "Local"),
		maxPlayers: Math.max(1, maxPlayers),
		players: players,
		allowedUserIds: allowedUserIds,
		serverId: "",
		host: "",
		port: 0,
		protocol: "UDP",
		connectionToken: createConnectionToken(matchId),
		errorMessage: "",
		createdAtMs: now,
		updatedAtMs: now,
		lastHeartbeatMs: 0,
	};

	return requestConfiguredOrchestratorAllocation(ctx, nk, logger, allocation);
}

function findRequestedAllocation(nk: nkruntime.Nakama, provider: MatchServerProvider): MatchServerAllocationRecord | null {
	var allocations = listAllocations(nk);
	var selected: MatchServerAllocationRecord | null = null;
	var now = Date.now();

	for (var i = 0; i < allocations.length; i++) {
		if (allocations[i].status !== "Requested") {
			continue;
		}
		if (allocations[i].provider !== provider) {
			continue;
		}

		if (provider === "LocalDev" && now - allocations[i].createdAtMs > MATCH_SERVER_REQUEST_STALE_MS) {
			markAllocationFailed(nk, allocations[i], "LocalDev allocation request expired before a server became available.");
			continue;
		}

		if (selected == null || allocations[i].createdAtMs > selected.createdAtMs) {
			selected = allocations[i];
		}
	}

	return selected;
}

function assignRequestedAllocationToServer(
	nk: nkruntime.Nakama,
	server: MatchServerRecord,
): MatchServerAllocationRecord | null {
	var allocation = findRequestedAllocation(nk, server.provider);
	if (allocation == null) {
		return null;
	}

	return assignServerToAllocation(nk, allocation, server);
}

function logMatchServerAvailable(logger: nkruntime.Logger, source: string, server: MatchServerRecord, extra: string): void {
	logger.info(
		"Match server available. source=" +
			source +
			" serverId=" +
			server.serverId +
			" provider=" +
			server.provider +
			" status=" +
			server.status +
			" public=" +
			server.host +
			":" +
			server.port +
			" currentAllocationId=" +
			server.currentAllocationId +
			" currentMatchId=" +
			server.currentMatchId +
			extra,
	);
}

function logMatchServerAssigned(logger: nkruntime.Logger, source: string, server: MatchServerRecord, allocation: MatchServerAllocationRecord): void {
	logger.info(
		"Match server assigned. source=" +
			source +
			" serverId=" +
			server.serverId +
			" provider=" +
			server.provider +
			" matchId=" +
			allocation.matchId +
			" allocationId=" +
			allocation.allocationId +
			" status=" +
			allocation.status +
			" public=" +
			allocation.host +
			":" +
			allocation.port,
	);
}

function logMatchServerReady(logger: nkruntime.Logger, server: MatchServerRecord, allocation: MatchServerAllocationRecord): void {
	logger.info(
		"Match server ready. serverId=" +
			server.serverId +
			" provider=" +
			server.provider +
			" matchId=" +
			allocation.matchId +
			" allocationId=" +
			allocation.allocationId +
			" status=" +
			allocation.status +
			" public=" +
			server.host +
			":" +
			server.port +
			" protocol=" +
			server.protocol,
	);
}

function logNoAvailableServer(logger: nkruntime.Logger, nk: nkruntime.Nakama, provider: MatchServerProvider, source: string): void {
	var servers = listServers(nk);
	var details: string[] = [];
	for (var i = 0; i < servers.length; i++) {
		if (servers[i].provider !== provider) {
			continue;
		}

		details.push(
			servers[i].serverId +
				":" +
				servers[i].status +
				":alloc=" +
				servers[i].currentAllocationId +
				":match=" +
				servers[i].currentMatchId,
		);
	}

	logger.info(
		"No available match server. source=" +
			source +
			" provider=" +
			provider +
			" candidates=" +
			(details.length > 0 ? details.join(",") : "none"),
	);
}

function refreshAllocationStaleness(
	ctx: nkruntime.Context,
	nk: nkruntime.Nakama,
	allocation: MatchServerAllocationRecord,
): MatchServerAllocationRecord {
	if (allocation.provider === "EdgegapCloud") {
		return refreshEdgegapDeploymentStatus(ctx, nk, allocation);
	}

	if (allocation.provider === "LocalDev" && !localDevOrchestrationAllowed(ctx)) {
		return markAllocationFailed(nk, allocation, "LocalDev allocation is disabled outside explicit local Docker orchestration.");
	}

	if (!allocation.serverId || allocation.status === "Failed" || allocation.status === "Released") {
		return allocation;
	}

	var server = readServer(nk, allocation.serverId);
	if (server == null) {
		return markAllocationFailed(nk, allocation, "Assigned server is missing.");
	}

	if (isServerStale(server, Date.now())) {
		server.status = "Failed";
		writeServer(nk, server);
		return markAllocationFailed(nk, allocation, "Dedicated server heartbeat timed out.");
	}

	return allocation;
}

function parseMatchServerRequest(payload: string): MatchServerRequest {
	var parsed = parseJsonPayload(payload);

	return {
		matchId: optionalString(parsed.matchId, ""),
		allocationId: optionalString(parsed.allocationId, ""),
		source: optionalString(parsed.source, "RandomQueue") as MatchSource,
		roomId: optionalString(parsed.roomId, ""),
		mapId: Math.max(0, optionalInteger(parsed.mapId, 1)),
		mapName: optionalString(parsed.mapName, "Classic Garden"),
		region: optionalString(parsed.region, "Local"),
		maxPlayers: Math.max(1, optionalInteger(parsed.maxPlayers, 4)),
		serverId: optionalString(parsed.serverId, ""),
		host: optionalString(parsed.host, "127.0.0.1"),
		port: Math.max(0, optionalInteger(parsed.port, 5000)),
		protocol: optionalString(parsed.protocol, "UDP"),
		status: optionalString(parsed.status, ""),
		serverSecret: optionalString(parsed.serverSecret, ""),
		reason: optionalString(parsed.reason, ""),
	};
}

function parseSettlementRequest(payload: string): MatchSettlementRequest {
	var parsed = parseJsonPayload(payload);
	var results: MatchSettlementPlayerRequest[] = [];

	if (Array.isArray(parsed.results)) {
		for (var i = 0; i < parsed.results.length; i++) {
			var raw = parsed.results[i] || {};
			results.push({
				userId: optionalString(raw.userId, ""),
				placement: Math.max(1, optionalInteger(raw.placement, i + 1)),
				kills: Math.max(0, optionalInteger(raw.kills, 0)),
				deaths: Math.max(0, optionalInteger(raw.deaths, 0)),
				disconnected: raw.disconnected === true,
			});
		}
	}

	return {
		serverId: optionalString(parsed.serverId, ""),
		allocationId: optionalString(parsed.allocationId, ""),
		matchId: optionalString(parsed.matchId, ""),
		serverSecret: optionalString(parsed.serverSecret, ""),
		results: results,
	};
}

function settlementResponse(
	settlement: MatchSettlementRecord | null,
	success?: boolean,
	errorMessage?: string,
): MatchSettlementResponse {
	return {
		success: success === true || (success !== false && settlement != null),
		errorMessage: errorMessage || "",
		settlementId: settlement != null ? settlement.settlementId : "",
		matchId: settlement != null ? settlement.matchId : "",
		allocationId: settlement != null ? settlement.allocationId : "",
		status: settlement != null ? settlement.status : "",
		rewards: settlement != null ? settlement.rewards : [],
	};
}

function calculateSettlementReward(result: MatchSettlementPlayerRequest, maxPlayers: number): MatchSettlementReward {
	var placement = Math.max(1, result.placement);
	var placementCoins = 2;
	var trophies = 0;

	if (placement === 1) {
		placementCoins = 10;
		trophies = 8;
	} else if (placement === 2) {
		placementCoins = 6;
		trophies = 4;
	} else if (placement === 3) {
		placementCoins = 4;
		trophies = 1;
	} else if (placement >= maxPlayers) {
		placementCoins = 2;
		trophies = -1;
	}

	if (result.disconnected) {
		trophies = Math.min(trophies, 0);
	}

	return {
		userId: result.userId,
		placement: placement,
		kills: result.kills,
		deaths: result.deaths,
		coinsDelta: 2 + placementCoins + result.kills,
		trophiesDelta: trophies,
		experienceDelta: 5 + result.kills * 2,
		winsDelta: placement === 1 ? 1 : 0,
		matchesPlayedDelta: 1,
	};
}

function applySettlementReward(nk: nkruntime.Nakama, reward: MatchSettlementReward): void {
	mutateProgression(
		nk,
		reward.userId,
		function (current: PlayerProgression): PlayerProgression {
			current.coins = Math.max(0, current.coins + reward.coinsDelta);
			current.gold = current.coins;
			current.trophies = Math.max(0, current.trophies + reward.trophiesDelta);
			current.experience = Math.max(0, current.experience + reward.experienceDelta);
			current.matchStats.matchesPlayed += reward.matchesPlayedDelta;
			current.matchStats.wins += reward.winsDelta;
			current.matchStats.kills += reward.kills;
			current.matchStats.deaths += reward.deaths;
			return current;
		},
	);
}

function generateRoomCode(): string {
	var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
	var code = "";

	for (var i = 0; i < 4; i++) {
		code += chars.charAt(Math.floor(Math.random() * chars.length));
	}

	return code;
}

function memberCount(state: CustomLobbyState): number {
	var count = 0;

	for (var userId in state.members) {
		if (state.members.hasOwnProperty(userId)) {
			count++;
		}
	}

	return count;
}

function lobbyRoomDto(state: CustomLobbyState, matchId: string): LobbyRoomDto {
	return {
		roomId: state.roomId,
		roomName: state.roomName,
		mapId: state.mapId,
		mapName: state.mapName,
		currentPlayers: memberCount(state),
		maxPlayers: state.maxPlayers,
		pingMs: 0,
		region: state.region,
		isPrivate: state.isPrivate,
		hostPlayerId: state.hostUserId,
		matchId: matchId,
		status: state.status,
		allocationId: "",
		serverStatus: "",
	};
}

function lobbyLabel(state: CustomLobbyState, matchId: string): string {
	return JSON.stringify({
		type: "custom_lobby",
		roomId: state.roomId,
		roomName: state.roomName,
		mapId: state.mapId,
		mapName: state.mapName,
		currentPlayers: memberCount(state),
		maxPlayers: state.maxPlayers,
		region: state.region,
		isPrivate: state.isPrivate,
		hostPlayerId: state.hostUserId,
		matchId: matchId,
		status: state.status,
		allocationId: "",
		serverStatus: "",
	});
}

function lobbyDtoFromMatch(match: nkruntime.Match): LobbyRoomDto | null {
	var label: any;

	try {
		label = JSON.parse(match.label || "{}");
	} catch (_) {
		return null;
	}

	if (!label || label.type !== "custom_lobby") {
		return null;
	}

	return {
		roomId: optionalString(label.roomId, match.matchId),
		roomName: optionalString(label.roomName, "Lobby"),
		mapId: optionalInteger(label.mapId, 1),
		mapName: optionalString(label.mapName, "Classic Garden"),
		currentPlayers: optionalInteger(label.currentPlayers, match.size),
		maxPlayers: clampInteger(label.maxPlayers, 4, 2, 4),
		pingMs: 0,
		region: optionalString(label.region, "Local"),
		isPrivate: label.isPrivate === true,
		hostPlayerId: optionalString(label.hostPlayerId, ""),
		matchId: match.matchId,
		status: optionalString(label.status, "Open") as LobbyStatus,
		allocationId: optionalString(label.allocationId, ""),
		serverStatus: optionalString(label.serverStatus, ""),
	};
}

function enrichLobbyRoomWithAllocation(
	nk: nkruntime.Nakama,
	room: LobbyRoomDto | null,
): LobbyRoomDto | null {
	if (room == null || !room.matchId) {
		return room;
	}

	var allocation = readAllocation(nk, room.matchId);
	if (allocation == null) {
		return room;
	}

	room.allocationId = allocation.allocationId;
	room.serverStatus = allocation.status;
	return room;
}

function parseListRoomsRequest(payload: string): ListRoomsRequest {
	var parsed = parseJsonPayload(payload);

	return {
		page: Math.max(0, optionalInteger(parsed.page, 0)),
		pageSize: clampInteger(parsed.pageSize, 5, 1, 20),
		region: optionalString(parsed.region, ""),
	};
}

function parseCreateLobbyRequest(payload: string): CreateLobbyRequest {
	var parsed = parseJsonPayload(payload);
	var roomId = optionalString(parsed.preferredRoomId, generateRoomCode())
		.toUpperCase()
		.substring(0, 8);

	return {
		roomName: optionalString(parsed.roomName, "Casual Room").substring(0, 32),
		mapId: Math.max(1, optionalInteger(parsed.mapId, 2)),
		mapName: optionalString(parsed.mapName, "Classic Garden").substring(0, 32),
		maxPlayers: clampInteger(parsed.maxPlayers, 4, 2, 4),
		preferredRoomId: roomId,
	};
}

function parseJoinLobbyRequest(payload: string): JoinLobbyRequest {
	var parsed = parseJsonPayload(payload);

	return {
		roomId: optionalString(parsed.roomId, "").toUpperCase(),
		matchId: optionalString(parsed.matchId, ""),
		roomCode: optionalString(parsed.roomCode, "").toUpperCase(),
		password: optionalString(parsed.password, "").toUpperCase(),
	};
}

function parseStartLobbyRequest(payload: string): StartLobbyRequest {
	var parsed = parseJsonPayload(payload);

	return {
		roomId: optionalString(parsed.roomId, "").toUpperCase(),
		matchId: optionalString(parsed.matchId, ""),
	};
}

function parseRandomQueueRequest(payload: string): RandomQueueRequest {
	var parsed = parseJsonPayload(payload);

	return {
		mapId: Math.max(0, optionalInteger(parsed.mapId, 0)),
		mapName: optionalString(parsed.mapName, "Random").substring(0, 32),
		region: optionalString(parsed.region, "Local").substring(0, 32),
		maxPlayers: 4,
		username: optionalString(parsed.username, ""),
		displayName: optionalString(parsed.displayName, ""),
	};
}

function parseRandomQueueTicketRequest(payload: string): string {
	var parsed = parseJsonPayload(payload);
	return optionalString(parsed.ticketId, "");
}

function listCustomLobbyMatches(nk: nkruntime.Nakama): nkruntime.Match[] {
	return nk.matchList(100, true, null, null, null, null);
}

function findLobbyMatch(
	nk: nkruntime.Nakama,
	roomId: string,
	matchId: string,
): nkruntime.Match | null {
	var matches = listCustomLobbyMatches(nk);
	var normalizedRoomId = roomId ? roomId.toUpperCase() : "";

	for (var i = 0; i < matches.length; i++) {
		var match = matches[i];

		if (matchId && match.matchId === matchId) {
			return match;
		}

		var room = lobbyDtoFromMatch(match);
		if (room != null && normalizedRoomId && room.roomId === normalizedRoomId) {
			return match;
		}
	}

	return null;
}

function findRandomQueueMatch(nk: nkruntime.Nakama): nkruntime.Match | null {
	var matches = nk.matchList(100, true, null, null, null, null);

	for (var i = 0; i < matches.length; i++) {
		var label: any;

		try {
			label = JSON.parse(matches[i].label || "{}");
		} catch (_) {
			continue;
		}

		if (label && label.type === "random_queue") {
			return matches[i];
		}
	}

	return null;
}

function getOrCreateRandomQueueMatchId(nk: nkruntime.Nakama): string {
	var match = findRandomQueueMatch(nk);

	if (match != null) {
		return match.matchId;
	}

	return nk.matchCreate("random_queue", {});
}

function signalLobby(
	nk: nkruntime.Nakama,
	matchId: string,
	request: LobbyActionRequest,
): LobbySignalResponse {
	var response = JSON.parse(nk.matchSignal(matchId, JSON.stringify(request)) || "{}") as LobbySignalResponse;

	if (typeof response.success !== "boolean") {
		return { success: false, errorMessage: "Lobby returned an invalid response.", room: null };
	}

	return response;
}

function signalRandomQueue(
	nk: nkruntime.Nakama,
	request: RandomQueueActionRequest,
): RandomQueueStatus {
	var matchId = getOrCreateRandomQueueMatchId(nk);
	var response = JSON.parse(nk.matchSignal(matchId, JSON.stringify(request)) || "{}") as RandomQueueStatus;

	if (typeof response.success !== "boolean") {
		return randomQueueDefaultStatus(false, "Queue returned an invalid response.");
	}

	return response;
}

function lobbySignalSuccess(state: CustomLobbyState, matchId: string): string {
	var members: LobbyMember[] = [];
	for (var userId in state.members) {
		if (state.members.hasOwnProperty(userId)) {
			members.push(state.members[userId]);
		}
	}

	return JSON.stringify({
		success: true,
		errorMessage: "",
		room: lobbyRoomDto(state, matchId),
		members: members,
	});
}

function lobbySignalFailure(errorMessage: string): string {
	return JSON.stringify({
		success: false,
		errorMessage: errorMessage,
		room: null,
	});
}

function customLobbyMatchInit(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	params: { [key: string]: any },
): { state: CustomLobbyState; tickRate: number; label: string } {
	var roomId = optionalString(params.roomId, generateRoomCode()).toUpperCase();
	var state: CustomLobbyState = {
		roomId: roomId,
		roomName: optionalString(params.roomName, "Casual Room"),
		mapId: Math.max(1, optionalInteger(params.mapId, 2)),
		mapName: optionalString(params.mapName, "Classic Garden"),
		maxPlayers: clampInteger(params.maxPlayers, 4, 2, 4),
		isPrivate: true,
		hostUserId: optionalString(params.hostUserId, ""),
		region: optionalString(params.region, "Local"),
		status: "Open",
		members: {},
		reservedUserIds: {},
	};

	if (state.hostUserId.length > 0) {
		state.reservedUserIds[state.hostUserId] = true;
	}

	return {
		state: state,
		tickRate: 1,
		label: lobbyLabel(state, ctx.matchId || ""),
	};
}

function customLobbyMatchJoinAttempt(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: CustomLobbyState,
	presence: nkruntime.Presence,
	metadata: { [key: string]: any },
): { state: CustomLobbyState; accept: boolean; rejectMessage?: string } {
	if (state.status !== "Open") {
		return { state: state, accept: false, rejectMessage: "Lobby is not open." };
	}

	if (state.members[presence.userId] !== undefined) {
		return { state: state, accept: true };
	}

	if (memberCount(state) >= state.maxPlayers) {
		return { state: state, accept: false, rejectMessage: "Lobby is full." };
	}

	if (
		presence.userId !== state.hostUserId &&
		state.reservedUserIds[presence.userId] !== true
	) {
		return { state: state, accept: false, rejectMessage: "Join was not reserved." };
	}

	return { state: state, accept: true };
}

function customLobbyMatchJoin(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: CustomLobbyState,
	presences: nkruntime.Presence[],
): { state: CustomLobbyState } {
	for (var i = 0; i < presences.length; i++) {
		var presence = presences[i];
		state.members[presence.userId] = {
			userId: presence.userId,
			username: presence.username || "",
			displayName: presence.username || presence.userId,
		};
		delete state.reservedUserIds[presence.userId];
	}

	dispatcher.matchLabelUpdate(lobbyLabel(state, ctx.matchId || ""));
	return { state: state };
}

function customLobbyMatchLeave(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: CustomLobbyState,
	presences: nkruntime.Presence[],
): { state: CustomLobbyState } | null {
	for (var i = 0; i < presences.length; i++) {
		var presence = presences[i];
		delete state.members[presence.userId];

		if (presence.userId === state.hostUserId && state.status === "Open") {
			state.status = "Closed";
		}
	}

	dispatcher.matchLabelUpdate(lobbyLabel(state, ctx.matchId || ""));
	return { state: state };
}

function customLobbyMatchLoop(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: CustomLobbyState,
	messages: nkruntime.MatchMessage[],
): { state: CustomLobbyState } | null {
	if (state.status === "Closed" && memberCount(state) === 0) {
		return null;
	}

	return { state: state };
}

function customLobbyMatchTerminate(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: CustomLobbyState,
	graceSeconds: number,
): { state: CustomLobbyState } | null {
	state.status = "Closed";
	dispatcher.matchLabelUpdate(lobbyLabel(state, ctx.matchId || ""));
	return { state: state };
}

function customLobbyMatchSignal(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: CustomLobbyState,
	data: string,
): { state: CustomLobbyState; data?: string } {
	var request = parseJsonPayload(data) as LobbyActionRequest;
	var matchId = ctx.matchId || "";

	if (request.action === "get") {
		return { state: state, data: lobbySignalSuccess(state, matchId) };
	}

	if (request.action === "reserve_join") {
		if (state.status !== "Open") {
			return { state: state, data: lobbySignalFailure("Lobby is not open.") };
		}

		if (memberCount(state) >= state.maxPlayers && !state.members[request.userId]) {
			return { state: state, data: lobbySignalFailure("Lobby is full.") };
		}

		if (optionalString(request.password || request.roomId, "").toUpperCase() !== state.roomId) {
			return { state: state, data: lobbySignalFailure("Incorrect room code.") };
		}

		state.reservedUserIds[request.userId] = true;
		dispatcher.matchLabelUpdate(lobbyLabel(state, matchId));
		return { state: state, data: lobbySignalSuccess(state, matchId) };
	}

	if (request.action === "leave") {
		delete state.members[request.userId];

		if (request.userId === state.hostUserId && state.status === "Open") {
			state.status = "Closed";
		}

		dispatcher.matchLabelUpdate(lobbyLabel(state, matchId));
		return { state: state, data: lobbySignalSuccess(state, matchId) };
	}

	if (request.action === "start") {
		if (request.userId !== state.hostUserId) {
			return { state: state, data: lobbySignalFailure("Only the host can start the match.") };
		}

		if (state.status !== "Open") {
			return { state: state, data: lobbySignalFailure("Lobby is not open.") };
		}

		state.status = "Starting";
		dispatcher.matchLabelUpdate(lobbyLabel(state, matchId));
		return { state: state, data: lobbySignalSuccess(state, matchId) };
	}

	return { state: state, data: lobbySignalFailure("Unknown lobby action.") };
}

var customLobbyMatchHandler: nkruntime.MatchHandler<CustomLobbyState> = {
	matchInit: customLobbyMatchInit,
	matchJoinAttempt: customLobbyMatchJoinAttempt,
	matchJoin: customLobbyMatchJoin,
	matchLeave: customLobbyMatchLeave,
	matchLoop: customLobbyMatchLoop,
	matchTerminate: customLobbyMatchTerminate,
	matchSignal: customLobbyMatchSignal,
};

function randomQueueLabel(): string {
	return JSON.stringify({ type: "random_queue", status: "Open" });
}

function randomQueueDefaultStatus(
	success: boolean,
	errorMessage: string,
	ticket?: RandomQueueTicket | null,
	match?: RandomQueueMatchRecord | null,
	state?: RandomQueueState | null,
): RandomQueueStatus {
	var status = ticket != null ? ticket.status : "";
	var matchId = ticket != null ? ticket.matchId : "";
	var playerCount = 0;
	var maxPlayers = ticket != null ? ticket.maxPlayers : 4;
	var acceptedCount = 0;
	var matchDto: RandomMatchDto | null = null;

	if (match != null) {
		playerCount = match.players.length;
		maxPlayers = match.players.length > 0 && ticket != null ? ticket.maxPlayers : 4;
		for (var userId in match.acceptedUserIds) {
			if (match.acceptedUserIds.hasOwnProperty(userId) && match.acceptedUserIds[userId]) {
				acceptedCount++;
			}
		}
		matchDto = {
			matchId: match.matchId,
			roomId: match.roomId,
			status: match.status,
			players: match.players,
			allocationId: match.allocationId,
			serverStatus: match.serverStatus,
		};
	} else if (ticket != null && state != null && ticket.status === "Searching") {
		for (var i = 0; i < state.queue.length; i++) {
			var queuedTicket = state.ticketsById[state.queue[i]];
			if (
				queuedTicket != null &&
				queuedTicket.status === "Searching" &&
				queuedTicket.region === ticket.region &&
				queuedTicket.mapId === ticket.mapId
			) {
				playerCount++;
			}
		}
	}

	return {
		success: success,
		errorMessage: errorMessage,
		ticketId: ticket != null ? ticket.ticketId : "",
		status: status,
		matchId: matchId,
		playerCount: playerCount,
		maxPlayers: maxPlayers,
		acceptedCount: acceptedCount,
		allocationId: match != null ? match.allocationId : "",
		serverStatus: match != null ? match.serverStatus : "",
		match: matchDto,
	};
}

function randomQueueTicketStatus(
	state: RandomQueueState,
	ticket: RandomQueueTicket,
): RandomQueueStatus {
	var match =
		ticket.matchId && state.matchesById[ticket.matchId]
			? state.matchesById[ticket.matchId]
			: null;
	return randomQueueDefaultStatus(true, "", ticket, match, state);
}

function refreshRandomQueueTicketLifecycle(
	nk: nkruntime.Nakama,
	logger: nkruntime.Logger,
	state: RandomQueueState,
	ticket: RandomQueueTicket | null,
): void {
	if (ticket == null || !ticket.matchId) {
		return;
	}

	var match = state.matchesById[ticket.matchId];
	var allocation = readAllocation(nk, ticket.matchId);
	if (allocation != null && match != null) {
		match.allocationId = allocation.allocationId;
		match.serverStatus = allocation.status;
	}

	if (
		ticket.status === "Accepted" &&
		allocation != null &&
		(allocation.status === "Released" || allocation.status === "Failed")
	) {
		ticket.status = "Expired";
		ticket.accepted = false;
		delete state.ticketIdByUserId[ticket.userId];

		if (match != null) {
			match.status = allocation.status;
			match.serverStatus = allocation.status;
		}

		logger.info(
			"Random queue ticket completed. ticketId=" +
				ticket.ticketId +
				" userId=" +
				ticket.userId +
				" matchId=" +
				ticket.matchId +
				" allocationId=" +
				allocation.allocationId +
				" allocationStatus=" +
				allocation.status,
		);
	}
}

function generateRandomQueueId(prefix: string, state: RandomQueueState): string {
	state.sequence++;
	return prefix + "-" + state.sequence + "-" + Math.floor(Math.random() * 1000000);
}

function randomQueueMatchInit(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	params: { [key: string]: any },
): { state: RandomQueueState; tickRate: number; label: string } {
	logger.info(
		"Random queue runtime initialized. requiredPlayers=" +
			randomQueueRequiredPlayers(ctx, 4),
	);

	return {
		state: {
			queue: [],
			ticketsById: {},
			ticketIdByUserId: {},
			matchesById: {},
			sequence: 0,
		},
		tickRate: 1,
		label: randomQueueLabel(),
	};
}

function randomQueueMatchJoinAttempt(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: RandomQueueState,
	presence: nkruntime.Presence,
	metadata: { [key: string]: any },
): { state: RandomQueueState; accept: boolean; rejectMessage?: string } {
	return { state: state, accept: false, rejectMessage: "Random queue does not accept socket joins." };
}

function randomQueueMatchJoin(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: RandomQueueState,
	presences: nkruntime.Presence[],
): { state: RandomQueueState } {
	return { state: state };
}

function randomQueueMatchLeave(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: RandomQueueState,
	presences: nkruntime.Presence[],
): { state: RandomQueueState } | null {
	return { state: state };
}

function tryFormRandomQueueMatches(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	state: RandomQueueState,
	trigger: string,
): RandomQueueState {
	var grouped = true;

	while (grouped) {
		grouped = false;

		for (var i = 0; i < state.queue.length; i++) {
			var seed = state.ticketsById[state.queue[i]];
			if (seed == null || seed.status !== "Searching") {
				continue;
			}

			var requiredPlayers = randomQueueRequiredPlayers(ctx, seed.maxPlayers);
			var group: RandomQueueTicket[] = [];
			for (var j = i; j < state.queue.length; j++) {
				var candidate = state.ticketsById[state.queue[j]];
				if (
					candidate != null &&
					candidate.status === "Searching" &&
					candidate.mapId === seed.mapId &&
					candidate.region === seed.region &&
					candidate.maxPlayers === seed.maxPlayers
				) {
					group.push(candidate);
				}

				if (group.length >= requiredPlayers) {
					break;
				}
			}

			if (group.length < requiredPlayers) {
				continue;
			}

			var matchId = generateRandomQueueId("rqmatch", state);
			var players: RandomQueuePlayerDto[] = [];
			var acceptedUserIds: { [userId: string]: boolean } = {};

			for (var k = 0; k < group.length; k++) {
				group[k].status = "Accepted";
				group[k].matchId = matchId;
				group[k].accepted = true;
				players.push({
					userId: group[k].userId,
					username: group[k].username,
					displayName: group[k].displayName,
				});
				acceptedUserIds[group[k].userId] = true;
			}

			var randomMatch: RandomQueueMatchRecord = {
				matchId: matchId,
				roomId: "RQ-" + matchId.substring(matchId.length - 6).toUpperCase(),
				status: "Accepted",
				players: players,
				acceptedUserIds: acceptedUserIds,
				mapId: seed.mapId,
				mapName: seed.mapName,
				region: seed.region,
				maxPlayers: seed.maxPlayers,
				allocationId: "",
				serverStatus: "",
				createdAtMs: Date.now(),
			};
			state.matchesById[matchId] = randomMatch;

			var allocation = createOrGetAllocation(
				ctx,
				nk,
				logger,
				"RandomQueue",
				randomMatch.matchId,
				randomMatch.roomId,
				randomMatch.mapId,
				randomMatch.mapName,
				randomMatch.region,
				randomMatch.maxPlayers,
				matchServerPlayersFromRandom(nk, randomMatch.players),
			);
			randomMatch.allocationId = allocation.allocationId;
			randomMatch.serverStatus = allocation.status;

			logger.info(
				"Random queue group accepted and allocation requested. matchId=" +
					matchId +
					" roomId=" +
					randomMatch.roomId +
					" players=" +
					players.length +
					"/" +
					seed.maxPlayers +
					" required=" +
					requiredPlayers +
					" mapId=" +
					seed.mapId +
					" region=" +
					seed.region +
					" trigger=" +
					trigger +
					" allocationId=" +
					allocation.allocationId +
					" allocationStatus=" +
					allocation.status,
			);

			var compacted: string[] = [];
			for (var q = 0; q < state.queue.length; q++) {
				var queued = state.ticketsById[state.queue[q]];
				if (queued != null && queued.status === "Searching") {
					compacted.push(state.queue[q]);
				}
			}
			state.queue = compacted;
			grouped = true;
			break;
		}
	}

	return state;
}

function randomQueueMatchLoop(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: RandomQueueState,
	messages: nkruntime.MatchMessage[],
): { state: RandomQueueState } | null {
	state = tryFormRandomQueueMatches(ctx, logger, nk, state, "loop");
	return { state: state };
}

function randomQueueMatchTerminate(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: RandomQueueState,
	graceSeconds: number,
): { state: RandomQueueState } | null {
	return { state: state };
}

function randomQueueMatchSignal(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	dispatcher: nkruntime.MatchDispatcher,
	tick: number,
	state: RandomQueueState,
	data: string,
): { state: RandomQueueState; data?: string } {
	var request = parseJsonPayload(data) as RandomQueueActionRequest;

	if (request.action === "join") {
		var existingTicketId = state.ticketIdByUserId[request.userId];
		var existingTicket = existingTicketId ? state.ticketsById[existingTicketId] : null;
		refreshRandomQueueTicketLifecycle(nk, logger, state, existingTicket);

		if (
			existingTicket != null &&
			(existingTicket.status === "Searching" ||
				existingTicket.status === "MatchFound" ||
				existingTicket.status === "Accepted")
		) {
			return { state: state, data: JSON.stringify(randomQueueTicketStatus(state, existingTicket)) };
		}

		var ticket: RandomQueueTicket = {
			ticketId: generateRandomQueueId("ticket", state),
			userId: request.userId,
			username: optionalString(request.username, request.userId),
			displayName: optionalString(request.displayName, optionalString(request.username, request.userId)),
			status: "Searching",
			matchId: "",
			mapId: Math.max(0, optionalInteger(request.mapId, 0)),
			mapName: optionalString(request.mapName, "Random"),
			region: optionalString(request.region, "Local"),
			maxPlayers: 4,
			createdAtMs: Date.now(),
			accepted: false,
		};

		state.ticketsById[ticket.ticketId] = ticket;
		state.ticketIdByUserId[ticket.userId] = ticket.ticketId;
		state.queue.push(ticket.ticketId);
		logger.info(
			"Random queue ticket joined. ticketId=" +
				ticket.ticketId +
				" userId=" +
				ticket.userId +
				" mapId=" +
				ticket.mapId +
				" region=" +
				ticket.region +
				" queued=" +
				randomQueueDefaultStatus(true, "", ticket, null, state).playerCount +
				"/" +
				ticket.maxPlayers,
		);
		state = tryFormRandomQueueMatches(ctx, logger, nk, state, "join");
		return { state: state, data: JSON.stringify(randomQueueTicketStatus(state, ticket)) };
	}

	if (request.action === "poll") {
		var pollTicket = state.ticketsById[request.ticketId];
		if (pollTicket == null || pollTicket.userId !== request.userId) {
			logger.warn("Random queue poll rejected. ticketId=" + request.ticketId + " userId=" + request.userId);
			return {
				state: state,
				data: JSON.stringify(randomQueueDefaultStatus(false, "Queue ticket not found.", null, null, state)),
			};
		}

		refreshRandomQueueTicketLifecycle(nk, logger, state, pollTicket);
		if (pollTicket.status === "Searching" || pollTicket.status === "MatchFound") {
			logger.info(
				"Random queue poll received. ticketId=" +
					pollTicket.ticketId +
					" userId=" +
					pollTicket.userId +
					" status=" +
					pollTicket.status +
					" ageMs=" +
					(Date.now() - pollTicket.createdAtMs) +
					" requiredPlayers=" +
					randomQueueRequiredPlayers(ctx, pollTicket.maxPlayers),
			);
		}

		state = tryFormRandomQueueMatches(ctx, logger, nk, state, "poll");
		return { state: state, data: JSON.stringify(randomQueueTicketStatus(state, pollTicket)) };
	}

	if (request.action === "cancel") {
		var cancelTicket = state.ticketsById[request.ticketId];
		if (cancelTicket == null || cancelTicket.userId !== request.userId) {
			return {
				state: state,
				data: JSON.stringify(randomQueueDefaultStatus(false, "Queue ticket not found.", null, null, state)),
			};
		}

		if (cancelTicket.status === "Searching") {
			cancelTicket.status = "Cancelled";
			delete state.ticketIdByUserId[cancelTicket.userId];

			var remaining: string[] = [];
			for (var r = 0; r < state.queue.length; r++) {
				if (state.queue[r] !== cancelTicket.ticketId) {
					remaining.push(state.queue[r]);
				}
			}
			state.queue = remaining;
		} else {
			var cancelMatch = cancelTicket.matchId ? state.matchesById[cancelTicket.matchId] : null;
			return {
				state: state,
				data: JSON.stringify(randomQueueDefaultStatus(false, "Match already formed.", cancelTicket, cancelMatch, state)),
			};
		}

		return { state: state, data: JSON.stringify(randomQueueTicketStatus(state, cancelTicket)) };
	}

	if (request.action === "accept") {
		var acceptTicket = state.ticketsById[request.ticketId];
		if (acceptTicket == null || acceptTicket.userId !== request.userId) {
			return {
				state: state,
				data: JSON.stringify(randomQueueDefaultStatus(false, "Queue ticket not found.", null, null, state)),
			};
		}

		if (acceptTicket.status === "Accepted") {
			return { state: state, data: JSON.stringify(randomQueueTicketStatus(state, acceptTicket)) };
		}

		if (acceptTicket.status !== "MatchFound") {
			return {
				state: state,
				data: JSON.stringify(randomQueueDefaultStatus(false, "Match is not ready.", acceptTicket, null, state)),
			};
		}

		acceptTicket.status = "Accepted";
		acceptTicket.accepted = true;
		var record = state.matchesById[acceptTicket.matchId];
		if (record != null) {
			record.acceptedUserIds[acceptTicket.userId] = true;
			var acceptedCount = 0;
			for (var acceptedUserId in record.acceptedUserIds) {
				if (
					record.acceptedUserIds.hasOwnProperty(acceptedUserId) &&
					record.acceptedUserIds[acceptedUserId]
				) {
					acceptedCount++;
				}
			}

			logger.info(
				"Random queue ticket accepted. ticketId=" +
					acceptTicket.ticketId +
					" userId=" +
					acceptTicket.userId +
					" matchId=" +
					record.matchId +
					" accepted=" +
					acceptedCount +
					"/" +
					record.players.length +
					" allocationId=" +
					record.allocationId,
			);

			if (acceptedCount >= record.players.length && !record.allocationId) {
				logger.info("Random queue all players accepted; requesting match server. matchId=" + record.matchId + " players=" + record.players.length);
				var allocation = createOrGetAllocation(
					ctx,
					nk,
					logger,
					"RandomQueue",
					record.matchId,
					record.roomId,
					record.mapId,
					record.mapName,
					record.region,
					record.maxPlayers,
					matchServerPlayersFromRandom(nk, record.players),
				);
				record.allocationId = allocation.allocationId;
				record.serverStatus = allocation.status;
			} else if (record.allocationId) {
				var existingAllocation = readAllocation(nk, record.matchId);
				if (existingAllocation != null) {
					record.serverStatus = existingAllocation.status;
				}
			}
		}

		return { state: state, data: JSON.stringify(randomQueueTicketStatus(state, acceptTicket)) };
	}

	return {
		state: state,
		data: JSON.stringify(randomQueueDefaultStatus(false, "Unknown queue action.", null, null, state)),
	};
}

var randomQueueMatchHandler: nkruntime.MatchHandler<RandomQueueState> = {
	matchInit: randomQueueMatchInit,
	matchJoinAttempt: randomQueueMatchJoinAttempt,
	matchJoin: randomQueueMatchJoin,
	matchLeave: randomQueueMatchLeave,
	matchLoop: randomQueueMatchLoop,
	matchTerminate: randomQueueMatchTerminate,
	matchSignal: randomQueueMatchSignal,
};

function mutateProgression(
	nk: nkruntime.Nakama,
	userId: string,
	mutate: (progression: PlayerProgression) => PlayerProgression,
): PlayerProgression {
	var request = progressionReadRequest(userId);

	for (var attempt = 0; attempt < PROGRESSION_WRITE_RETRIES; attempt++) {
		var objects = nk.storageRead([request]);
		var current =
			objects.length > 0
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
	payload: string,
): string {
	return JSON.stringify({
		ok: true,
		service: "bommy-nakama",
		version: SERVER_VERSION,
		userId: ctx.userId || null,
		payloadLength: payload ? payload.length : 0,
		serverTimeMs: Date.now(),
	});
}

function rpcGetPlayerProgression(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	return JSON.stringify(readOrCreateProgression(nk, requireUserId(ctx)));
}

function rpcPurchaseCharacter(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseCharacterRequest(payload);
	var catalogEntry = CHARACTER_CATALOG[String(request.characterId)];

	var progression = mutateProgression(
		nk,
		userId,
		(current: PlayerProgression): PlayerProgression => {
			if (current.ownedCharacterIds.indexOf(request.characterId) >= 0) {
				return current;
			}

			if (current.coins < catalogEntry.price) {
				throw new Error("Insufficient coins.");
			}

			current.coins -= catalogEntry.price;
			current.gold = current.coins;
			current.ownedCharacterIds.push(request.characterId);
			current.ownedCharacterIds.sort((a: number, b: number): number => a - b);
			return current;
		},
	);

	return JSON.stringify(progression);
}

function rpcSelectCharacter(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseCharacterRequest(payload);

	var progression = mutateProgression(
		nk,
		userId,
		function (current: PlayerProgression): PlayerProgression {
			if (current.ownedCharacterIds.indexOf(request.characterId) < 0) {
				throw new Error("Character is not owned.");
			}

			current.selectedCharacterId = request.characterId;
			return current;
		},
	);

	return JSON.stringify(progression);
}

function rpcGrantMatchRewards(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseMatchRewardRequest(payload);

	var progression = mutateProgression(
		nk,
		userId,
		function (current: PlayerProgression): PlayerProgression {
			current.coins = Math.max(0, current.coins + request.coinsDelta);
			current.gold = current.coins;
			current.trophies = Math.max(0, current.trophies + request.trophiesDelta);
			current.experience = Math.max(
				0,
				current.experience + request.experienceDelta,
			);
			current.matchStats.matchesPlayed += request.matchesPlayedDelta;
			current.matchStats.wins += request.winsDelta;
			current.matchStats.kills += request.killsDelta;
			current.matchStats.deaths += request.deathsDelta;
			return current;
		},
	);

	return JSON.stringify(progression);
}

function rpcSettleMatch(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseSettlementRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	if (!request.matchId) {
		return JSON.stringify(settlementResponse(null, false, "matchId is required."));
	}

	var existing = readSettlement(nk, request.matchId);
	if (existing != null) {
		logger.info(
			"Match settlement replayed. matchId=" +
				existing.matchId +
				" allocationId=" +
				existing.allocationId +
				" serverId=" +
				existing.serverId +
				" rewards=" +
				existing.rewards.length,
		);
		return JSON.stringify(settlementResponse(existing));
	}

	var allocation = readAllocation(nk, request.matchId);
	if (allocation == null) {
		return JSON.stringify(settlementResponse(null, false, "Allocation not found."));
	}

	if (request.allocationId && allocation.allocationId !== request.allocationId) {
		return JSON.stringify(settlementResponse(null, false, "Allocation id does not match."));
	}

	if (request.serverId && allocation.serverId && allocation.serverId !== request.serverId) {
		return JSON.stringify(settlementResponse(null, false, "Server does not own this allocation."));
	}

	if (allocation.status === "Released" || allocation.status === "Failed") {
		return JSON.stringify(settlementResponse(null, false, "Allocation is not settleable."));
	}

	if (request.results.length <= 0) {
		return JSON.stringify(settlementResponse(null, false, "Settlement results are required."));
	}

	logger.info(
		"Match settlement requested. matchId=" +
			allocation.matchId +
			" allocationId=" +
			allocation.allocationId +
			" serverId=" +
			allocation.serverId +
			" source=" +
			allocation.source +
			" results=" +
			request.results.length,
	);

	var seen: { [userId: string]: boolean } = {};
	var rewards: MatchSettlementReward[] = [];

	for (var i = 0; i < request.results.length; i++) {
		var result = request.results[i];
		if (!result.userId) {
			return JSON.stringify(settlementResponse(null, false, "Settlement result userId is required."));
		}

		if (seen[result.userId]) {
			return JSON.stringify(settlementResponse(null, false, "Duplicate player in settlement."));
		}

		if (Object.keys(allocation.allowedUserIds).length > 0 && !allocation.allowedUserIds[result.userId]) {
			return JSON.stringify(settlementResponse(null, false, "Player is not assigned to this match."));
		}

		seen[result.userId] = true;
		rewards.push(calculateSettlementReward(result, allocation.maxPlayers));
	}

	allocation.status = "Settling";
	writeAllocation(nk, allocation);

	for (var r = 0; r < rewards.length; r++) {
		applySettlementReward(nk, rewards[r]);
	}

	var settlement: MatchSettlementRecord = {
		settlementId: settlementKey(request.matchId),
		allocationId: allocation.allocationId,
		matchId: allocation.matchId,
		serverId: allocation.serverId,
		status: "Settled",
		rewards: rewards,
		createdAtMs: Date.now(),
	};
	writeSettlement(nk, settlement);

	allocation.status = "Released";
	allocation.errorMessage = "";
	writeAllocation(nk, allocation);
	logger.info(
		"Match settlement completed. matchId=" +
			settlement.matchId +
			" allocationId=" +
			settlement.allocationId +
			" serverId=" +
			settlement.serverId +
			" rewards=" +
			settlement.rewards.length +
			" allocationStatus=Released",
	);

	return JSON.stringify(settlementResponse(settlement));
}

function rpcListLobbies(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	requireUserId(ctx);

	var request = parseListRoomsRequest(payload);
	var matches = listCustomLobbyMatches(nk);
	var rooms: LobbyRoomDto[] = [];

	for (var i = 0; i < matches.length; i++) {
		var room = lobbyDtoFromMatch(matches[i]);

		if (room == null) {
			continue;
		}

		if (room.status !== "Open") {
			continue;
		}

		if (request.region && room.region !== request.region) {
			continue;
		}

		rooms.push(room);
	}

	rooms.sort(function (a: LobbyRoomDto, b: LobbyRoomDto): number {
		if (a.roomName < b.roomName) {
			return -1;
		}

		if (a.roomName > b.roomName) {
			return 1;
		}

		return 0;
	});

	var start = request.page * request.pageSize;
	var end = Math.min(start + request.pageSize, rooms.length);

	return JSON.stringify({
		rooms: rooms.slice(start, end),
		totalCount: rooms.length,
		page: request.page,
		pageSize: request.pageSize,
	} as ListRoomsResponse);
}

function rpcGetLobby(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	requireUserId(ctx);

	var request = parseJoinLobbyRequest(payload);
	var match = findLobbyMatch(nk, request.roomId, request.matchId);

	if (match == null) {
		return JSON.stringify({ success: false, errorMessage: "Room not found.", room: null });
	}

	var response = signalLobby(nk, match.matchId, {
		action: "get",
		userId: ctx.userId || "",
	});

	return JSON.stringify({
		success: response.success,
		errorMessage: response.errorMessage,
		room: enrichLobbyRoomWithAllocation(nk, response.room),
	});
}

function rpcCreateLobby(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseCreateLobbyRequest(payload);
	var roomId = request.preferredRoomId;

	if (findLobbyMatch(nk, roomId, "") != null) {
		roomId = generateRoomCode();
	}

	var matchId = nk.matchCreate("custom_lobby", {
		roomId: roomId,
		roomName: request.roomName,
		mapId: request.mapId,
		mapName: request.mapName,
		maxPlayers: request.maxPlayers,
		hostUserId: userId,
		region: "Local",
	});

	var match = nk.matchGet(matchId);
	var room = match != null ? lobbyDtoFromMatch(match) : null;

	if (room == null) {
		throw new Error("Lobby was created but could not be read.");
	}

	return JSON.stringify({
		success: true,
		errorMessage: "",
		room: room,
	});
}

function rpcJoinLobby(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseJoinLobbyRequest(payload);
	var match = findLobbyMatch(nk, request.roomId, request.matchId);

	if (match == null) {
		return JSON.stringify({ success: false, errorMessage: "Room not found.", room: null });
	}

	var response = signalLobby(nk, match.matchId, {
		action: "reserve_join",
		userId: userId,
		roomId: request.roomId,
		password: request.password,
	});

	return JSON.stringify({
		success: response.success,
		errorMessage: response.errorMessage,
		room: response.room,
	});
}

function rpcJoinLobbyByCode(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var parsed = parseJoinLobbyRequest(payload);
	return rpcJoinLobby(
		ctx,
		logger,
		nk,
		JSON.stringify({
			roomId: parsed.roomCode || parsed.roomId,
			password: parsed.password || parsed.roomCode || parsed.roomId,
		}),
	);
}

function rpcLeaveLobby(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseJoinLobbyRequest(payload);
	var match = findLobbyMatch(nk, request.roomId, request.matchId);

	if (match == null) {
		return JSON.stringify({ success: true, errorMessage: "", room: null });
	}

	var response = signalLobby(nk, match.matchId, {
		action: "leave",
		userId: userId,
	});

	return JSON.stringify({
		success: response.success,
		errorMessage: response.errorMessage,
		room: response.room,
	});
}

function rpcStartLobbyMatch(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseStartLobbyRequest(payload);
	var match = findLobbyMatch(nk, request.roomId, request.matchId);

	if (match == null) {
		return JSON.stringify({
			success: false,
			errorMessage: "Room not found.",
			matchId: "",
			status: "",
		});
	}

	var response = signalLobby(nk, match.matchId, {
		action: "start",
		userId: userId,
	});
	var room = response.room;
	var allocation: MatchServerAllocationRecord | null = null;

	if (response.success && room != null) {
		logger.info(
			"Custom lobby start accepted; requesting match server. roomId=%s matchId=%s hostUserId=%s players=%s",
			room.roomId,
			room.matchId,
			userId,
			response.members != null ? response.members.length : 0,
		);
		allocation = createOrGetAllocation(
			ctx,
			nk,
			logger,
			"CustomLobby",
			room.matchId,
			room.roomId,
			room.mapId,
			room.mapName,
			room.region,
			room.maxPlayers,
			matchServerPlayersFromLobby(
				nk,
				response.members,
				userId,
				optionalString((ctx as any).username, userId),
			),
		);

		room.allocationId = allocation.allocationId;
		room.serverStatus = allocation.status;
	}

	return JSON.stringify({
		success: response.success,
		errorMessage: response.errorMessage,
		matchId: room != null ? room.matchId : "",
		status: room != null ? room.status : "",
		allocationId: allocation != null ? allocation.allocationId : "",
		serverStatus: allocation != null ? allocation.status : "",
	});
}

function rpcJoinRandomQueue(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseRandomQueueRequest(payload);
	var username = optionalString(request.username, optionalString((ctx as any).username, userId));
	var displayName = optionalString(request.displayName, username);

	return JSON.stringify(
		signalRandomQueue(nk, {
			action: "join",
			userId: userId,
			username: username,
			displayName: displayName,
			ticketId: "",
			mapId: request.mapId,
			mapName: request.mapName,
			region: request.region,
			maxPlayers: request.maxPlayers,
		}),
	);
}

function rpcPollRandomQueue(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var ticketId = parseRandomQueueTicketRequest(payload);

	return JSON.stringify(
		signalRandomQueue(nk, {
			action: "poll",
			userId: userId,
			username: "",
			displayName: "",
			ticketId: ticketId,
			mapId: 1,
			mapName: "",
			region: "",
			maxPlayers: 4,
		}),
	);
}

function rpcCancelRandomQueue(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var ticketId = parseRandomQueueTicketRequest(payload);

	return JSON.stringify(
		signalRandomQueue(nk, {
			action: "cancel",
			userId: userId,
			username: "",
			displayName: "",
			ticketId: ticketId,
			mapId: 1,
			mapName: "",
			region: "",
			maxPlayers: 4,
		}),
	);
}

function rpcAcceptRandomMatch(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var ticketId = parseRandomQueueTicketRequest(payload);

	return JSON.stringify(
		signalRandomQueue(nk, {
			action: "accept",
			userId: userId,
			username: "",
			displayName: "",
			ticketId: ticketId,
			mapId: 1,
			mapName: "",
			region: "",
			maxPlayers: 4,
		}),
	);
}

function rpcRequestMatchServer(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseMatchServerRequest(payload);

	if (!request.matchId) {
		return JSON.stringify(allocationResponse(null, false, "matchId is required."));
	}

	var existing = readAllocation(nk, request.matchId);
	if (existing != null) {
		if (
			existing.source === "RandomQueue" &&
			Object.keys(existing.allowedUserIds).length > 0 &&
			!existing.allowedUserIds[userId]
		) {
			return JSON.stringify(allocationResponse(null, false, "You are not assigned to this match."));
		}

		return JSON.stringify(allocationResponse(refreshAllocationStaleness(ctx, nk, existing)));
	}

	if (request.source === "RandomQueue") {
		return JSON.stringify(allocationResponse(null, false, "Random queue allocation is owned by queue runtime."));
	}

	var players: MatchServerPlayerDto[] = [
		{
			userId: userId,
			username: optionalString((ctx as any).username, userId),
			displayName: optionalString((ctx as any).username, userId),
			selectedCharacterId: selectedCharacterForUser(nk, userId),
		},
	];

	logger.info("Direct match server request received. matchId=%s source=%s userId=%s", request.matchId, request.source, userId);

	var allocation = createOrGetAllocation(
		ctx,
		nk,
		logger,
		request.source,
		request.matchId,
		request.roomId,
		request.mapId,
		request.mapName,
		request.region,
		request.maxPlayers,
		players,
	);

	return JSON.stringify(allocationResponse(allocation));
}

function rpcGetMatchServerStatus(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var userId = requireUserId(ctx);
	var request = parseMatchServerRequest(payload);
	var matchId = request.matchId;

	if (!matchId && request.allocationId) {
		var allocations = listAllocations(nk);
		for (var i = 0; i < allocations.length; i++) {
			if (allocations[i].allocationId === request.allocationId) {
				matchId = allocations[i].matchId;
				break;
			}
		}
	}

	if (!matchId) {
		return JSON.stringify(statusResponse(null, false, "matchId is required."));
	}

	var allocation = readAllocation(nk, matchId);
	if (allocation == null) {
		return JSON.stringify(statusResponse(null, false, "Allocation not found."));
	}

	if (
		allocation.source === "RandomQueue" &&
		Object.keys(allocation.allowedUserIds).length > 0 &&
		!allocation.allowedUserIds[userId]
	) {
		return JSON.stringify(statusResponse(null, false, "You are not assigned to this match."));
	}

	return JSON.stringify(statusResponse(refreshAllocationStaleness(ctx, nk, allocation)));
}

function rpcRegisterMatchServer(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseMatchServerRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	if (!request.serverId) {
		return JSON.stringify(allocationResponse(null, false, "serverId is required."));
	}

	var now = Date.now();
	var existing = readServer(nk, request.serverId);

	if (existing == null && !localDevOrchestrationAllowed(ctx)) {
		return JSON.stringify(allocationResponse(null, false, "Server registration requires an existing Edgegap allocation or explicit LocalDev orchestration."));
	}

	if (existing != null && existing.provider === "LocalDev" && !localDevOrchestrationAllowed(ctx)) {
		return JSON.stringify(allocationResponse(null, false, "LocalDev server registration is disabled outside explicit local Docker orchestration."));
	}

	var server: MatchServerRecord =
		existing != null
			? existing
			: {
					serverId: request.serverId,
					provider: "LocalDev",
					status: "Available",
					deploymentId: "",
					host: request.host,
					port: request.port,
					protocol: request.protocol,
					currentAllocationId: "",
					currentMatchId: "",
					createdAtMs: now,
					updatedAtMs: now,
					lastHeartbeatMs: now,
				};

	server.host = request.host;
	server.port = request.port;
	server.protocol = request.protocol;
	server.lastHeartbeatMs = now;

	if (request.status === "Available" && !request.allocationId && !request.matchId && server.currentAllocationId) {
		var staleAllocation = server.currentMatchId ? readAllocation(nk, server.currentMatchId) : null;
		if (staleAllocation != null && staleAllocation.status !== "Released") {
			staleAllocation.status = "Released";
			staleAllocation.errorMessage = "Server re-registered as idle and cleared stale assignment.";
			writeAllocation(nk, staleAllocation);
		}

		logger.warn(
			"Match server registration cleared stale assignment. serverId=" +
				server.serverId +
				" staleMatchId=" +
				server.currentMatchId +
				" staleAllocationId=" +
				server.currentAllocationId,
		);
		server.status = "Available";
		server.currentAllocationId = "";
		server.currentMatchId = "";
	} else if (!server.currentAllocationId || server.status === "Failed" || server.status === "Released") {
		server.status = "Available";
		server.currentAllocationId = "";
		server.currentMatchId = "";
	}

	writeServer(nk, server);
	if (server.status === "Available" && !server.currentAllocationId) {
		logMatchServerAvailable(logger, "register", server, "");
	}

	var assigned = server.provider === "LocalDev" && localDevOrchestrationAllowed(ctx) && server.status === "Available"
		? assignRequestedAllocationToServer(nk, server)
		: null;
	if (assigned != null) {
		logMatchServerAssigned(logger, "register", server, assigned);
		return JSON.stringify(allocationResponse(assigned));
	}

	return JSON.stringify({
		success: true,
		errorMessage: "",
		allocationId: "",
		matchId: "",
		source: "",
		provider: server.provider,
		status: server.status,
	});
}

function rpcClaimMatchLaunchConfig(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseMatchServerRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	var server = readServer(nk, request.serverId);
	if (server == null) {
		logger.warn("Match server launch config claim rejected. serverId=" + request.serverId + " reason=not_registered");
		return JSON.stringify({
			success: false,
			errorMessage: "Server is not registered.",
			allocationId: "",
			matchId: "",
			source: "",
			roomId: "",
			mapId: 0,
			mapName: "",
			region: "",
			maxPlayers: 0,
			players: [],
			nakamaHost: "127.0.0.1",
			nakamaPort: 7350,
			purrnetPort: 0,
		} as MatchLaunchConfigResponse);
	}

	var allocation: MatchServerAllocationRecord | null = null;
	if (server.currentMatchId) {
		allocation = readAllocation(nk, server.currentMatchId);
	}

	if (allocation == null && request.matchId) {
		allocation = readAllocation(nk, request.matchId);
	}

	if (allocation == null && server.provider === "LocalDev" && localDevOrchestrationAllowed(ctx) && server.status === "Available") {
		allocation = assignRequestedAllocationToServer(nk, server);
		if (allocation != null) {
			logMatchServerAssigned(logger, "claim", server, allocation);
		}
	}

	if (allocation == null) {
		if (request.matchId) {
			logger.info(
				"Match server launch config unavailable. serverId=" +
					server.serverId +
					" provider=" +
					server.provider +
					" status=" +
					server.status +
					" requestedMatchId=" +
					request.matchId,
			);
		}

		return JSON.stringify({
			success: false,
			errorMessage: "No allocation assigned.",
			allocationId: "",
			matchId: "",
			source: "",
			roomId: "",
			mapId: 0,
			mapName: "",
			region: "",
			maxPlayers: 0,
			players: [],
			nakamaHost: "127.0.0.1",
			nakamaPort: 7350,
			purrnetPort: 0,
		} as MatchLaunchConfigResponse);
	}

	server.currentAllocationId = allocation.allocationId;
	server.currentMatchId = allocation.matchId;
	server.status = allocation.status === "Requested" ? "Launching" : allocation.status;
	server.lastHeartbeatMs = Date.now();
	writeServer(nk, server);

	if (allocation.status === "Requested") {
		allocation.status = "Launching";
		allocation.serverId = server.serverId;
		allocation.host = server.host;
		allocation.port = server.port;
		allocation.protocol = server.protocol;
		writeAllocation(nk, allocation);
	}

	logger.info(
		"Match server launch config claimed. serverId=" +
			server.serverId +
			" provider=" +
			server.provider +
			" matchId=" +
			allocation.matchId +
			" allocationId=" +
			allocation.allocationId +
			" status=" +
			allocation.status +
			" public=" +
			server.host +
			":" +
			server.port,
	);

	return JSON.stringify({
		success: true,
		errorMessage: "",
		allocationId: allocation.allocationId,
		matchId: allocation.matchId,
		source: allocation.source,
		roomId: allocation.roomId,
		mapId: allocation.mapId,
		mapName: allocation.mapName,
		region: allocation.region,
		maxPlayers: allocation.maxPlayers,
		players: allocation.players,
		nakamaHost: "127.0.0.1",
		nakamaPort: 7350,
		purrnetPort: server.port,
	} as MatchLaunchConfigResponse);
}

function rpcMarkMatchServerReady(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseMatchServerRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	var server = readServer(nk, request.serverId);
	var allocation = request.matchId ? readAllocation(nk, request.matchId) : null;

	if (allocation == null && request.allocationId) {
		var allocations = listAllocations(nk);
		for (var i = 0; i < allocations.length; i++) {
			if (allocations[i].allocationId === request.allocationId) {
				allocation = allocations[i];
				break;
			}
		}
	}

	if (server == null || allocation == null) {
		logger.warn(
			"Match server ready rejected. serverId=" +
				request.serverId +
				" matchId=" +
				request.matchId +
				" allocationId=" +
				request.allocationId +
				" serverFound=" +
				(server != null) +
				" allocationFound=" +
				(allocation != null),
		);
		return JSON.stringify(statusResponse(null, false, "Server or allocation not found."));
	}

	if (server.provider === "LocalDev" && !localDevOrchestrationAllowed(ctx)) {
		logger.warn("Match server ready rejected. serverId=" + server.serverId + " provider=LocalDev reason=localdev_disabled");
		return JSON.stringify(statusResponse(null, false, "LocalDev server readiness is disabled outside explicit local Docker orchestration."));
	}

	var now = Date.now();
	server.status = "Ready";
	server.host = request.host;
	server.port = request.port;
	server.protocol = request.protocol;
	server.currentAllocationId = allocation.allocationId;
	server.currentMatchId = allocation.matchId;
	server.lastHeartbeatMs = now;

	allocation.status = "Ready";
	allocation.host = request.host;
	allocation.port = request.port;
	allocation.protocol = request.protocol;
	allocation.serverId = server.serverId;
	allocation.lastHeartbeatMs = now;
	allocation.errorMessage = "";

	writeServer(nk, server);
	writeAllocation(nk, allocation);
	logMatchServerReady(logger, server, allocation);
	return JSON.stringify(statusResponse(allocation));
}

function rpcServerHeartbeat(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseMatchServerRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	var server = readServer(nk, request.serverId);
	if (server == null) {
		return JSON.stringify(statusResponse(null, false, "Server is not registered."));
	}

	if (server.provider === "LocalDev" && !localDevOrchestrationAllowed(ctx)) {
		return JSON.stringify(statusResponse(null, false, "LocalDev server heartbeat is disabled outside explicit local Docker orchestration."));
	}

	var now = Date.now();
	var previousStatus = server.status;
	var previousAllocationId = server.currentAllocationId;
	server.lastHeartbeatMs = now;
	if (request.host) {
		server.host = request.host;
	}
	if (request.port > 0) {
		server.port = request.port;
	}

	var allocation: MatchServerAllocationRecord | null = server.currentMatchId ? readAllocation(nk, server.currentMatchId) : null;

	if (allocation != null) {
		allocation.lastHeartbeatMs = now;
		if (request.status === "InMatch" || request.status === "Ready" || request.status === "Settling") {
			allocation.status = request.status as MatchServerStatusName;
			server.status = allocation.status;
		}
		writeAllocation(nk, allocation);
	} else if (!server.currentAllocationId) {
		server.status = "Available";
	}

	writeServer(nk, server);
	if (
		request.status === "Available" &&
		server.status === "Available" &&
		!server.currentAllocationId &&
		(previousStatus !== "Available" || previousAllocationId)
	) {
		logMatchServerAvailable(
			logger,
			"heartbeat",
			server,
			" previousStatus=" + previousStatus + " previousAllocationId=" + previousAllocationId,
		);
	}

	return JSON.stringify(statusResponse(allocation || null, true, ""));
}

function rpcResetMatchServer(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseMatchServerRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	var server = readServer(nk, request.serverId);
	if (server == null) {
		return JSON.stringify(allocationResponse(null, false, "Server is not registered."));
	}

	var allocation = server.currentMatchId ? readAllocation(nk, server.currentMatchId) : null;
	if (allocation != null) {
		allocation.status = "Released";
		writeAllocation(nk, allocation);
	}

	server.status = "Available";
	server.currentAllocationId = "";
	server.currentMatchId = "";
	server.lastHeartbeatMs = Date.now();
	writeServer(nk, server);
	logMatchServerAvailable(
		logger,
		"reset",
		server,
		allocation != null
			? " releasedMatchId=" + allocation.matchId + " releasedAllocationId=" + allocation.allocationId + " reason=" + optionalString(request.reason, "")
			: " reason=" + optionalString(request.reason, ""),
	);

	var assigned = server.provider === "LocalDev" && !localDevOrchestrationAllowed(ctx)
		? null
		: assignRequestedAllocationToServer(nk, server);
	if (assigned != null) {
		logMatchServerAssigned(logger, "reset", server, assigned);
		return JSON.stringify(allocationResponse(assigned));
	}

	return JSON.stringify(allocationResponse(null, true, ""));
}

function rpcReleaseMatchServer(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	payload: string,
): string {
	var request = parseMatchServerRequest(payload);
	validateServerSecret(ctx, request.serverSecret);

	var server = readServer(nk, request.serverId);
	if (server != null) {
		server.status = "Released";
		server.currentAllocationId = "";
		server.currentMatchId = "";
		writeServer(nk, server);
	}

	if (request.matchId) {
		var allocation = readAllocation(nk, request.matchId);
		if (allocation != null) {
			stopEdgegapDeployment(ctx, nk, allocation);
			allocation.status = "Released";
			writeAllocation(nk, allocation);
		}
	}

	return JSON.stringify(allocationResponse(null, true, ""));
}

function InitModule(
	ctx: nkruntime.Context,
	logger: nkruntime.Logger,
	nk: nkruntime.Nakama,
	initializer: nkruntime.Initializer,
): void {
	initializer.registerRpc("healthcheck", rpcHealthcheck);
	initializer.registerRpc("get_player_progression", rpcGetPlayerProgression);
	initializer.registerRpc("purchase_character", rpcPurchaseCharacter);
	initializer.registerRpc("select_character", rpcSelectCharacter);
	initializer.registerRpc("grant_match_rewards", rpcGrantMatchRewards);
	initializer.registerRpc("settle_match", rpcSettleMatch);
	initializer.registerRpc("list_lobbies", rpcListLobbies);
	initializer.registerRpc("get_lobby", rpcGetLobby);
	initializer.registerRpc("create_lobby", rpcCreateLobby);
	initializer.registerRpc("join_lobby", rpcJoinLobby);
	initializer.registerRpc("join_lobby_by_code", rpcJoinLobbyByCode);
	initializer.registerRpc("leave_lobby", rpcLeaveLobby);
	initializer.registerRpc("start_lobby_match", rpcStartLobbyMatch);
	initializer.registerRpc("request_match_server", rpcRequestMatchServer);
	initializer.registerRpc("get_match_server_status", rpcGetMatchServerStatus);
	initializer.registerRpc("register_match_server", rpcRegisterMatchServer);
	initializer.registerRpc("claim_match_launch_config", rpcClaimMatchLaunchConfig);
	initializer.registerRpc("mark_match_server_ready", rpcMarkMatchServerReady);
	initializer.registerRpc("server_heartbeat", rpcServerHeartbeat);
	initializer.registerRpc("reset_match_server", rpcResetMatchServer);
	initializer.registerRpc("release_match_server", rpcReleaseMatchServer);
	initializer.registerRpc("join_random_queue", rpcJoinRandomQueue);
	initializer.registerRpc("cancel_random_queue", rpcCancelRandomQueue);
	initializer.registerRpc("poll_random_queue", rpcPollRandomQueue);
	initializer.registerRpc("accept_random_match", rpcAcceptRandomMatch);
	initializer.registerMatch("custom_lobby", customLobbyMatchHandler);
	initializer.registerMatch("random_queue", randomQueueMatchHandler);
	logger.info("Bommy Nakama TypeScript runtime loaded.");
	logger.info(
		"Match server orchestration provider=" +
			configuredMatchServerProvider(ctx) +
			" edgegapEnabled=" +
			readEdgegapConfig(ctx).enabled +
			" localDevAllowed=" +
			localDevOrchestrationAllowed(ctx) +
			" railwayRuntime=" +
			isRailwayRuntime(ctx),
	);
}

!InitModule && InitModule.bind(null);

