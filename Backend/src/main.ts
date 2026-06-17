var SERVER_VERSION = "0.1.0";

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

function InitModule(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  initializer: nkruntime.Initializer
): void {
  initializer.registerRpc("healthcheck", rpcHealthcheck);
  logger.info("Bommy Nakama TypeScript runtime loaded.");
}

!InitModule && InitModule.bind(null);

