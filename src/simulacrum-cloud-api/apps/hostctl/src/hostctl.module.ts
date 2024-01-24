import { Module } from "@nestjs/common";
import { EventsGateway } from "./events/events.gateway";
import { DbModule } from "@simulacrum/db";
import { HealthController } from "./health/health.controller";

@Module({
  imports: [DbModule],
  controllers: [HealthController],
  providers: [EventsGateway],
})
export class HostctlModule {}
