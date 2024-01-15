import { Module } from "@nestjs/common";
import { EventsGateway } from "./events/events.gateway";
import { DbModule } from "@simulacrum/db";

@Module({
  imports: [DbModule],
  controllers: [],
  providers: [EventsGateway],
})
export class HostctlModule {}
