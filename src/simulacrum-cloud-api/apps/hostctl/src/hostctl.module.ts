import { Module } from "@nestjs/common";
import { HostctlService } from "./hostctl.service";
import { EventsGateway } from "./events/events.gateway";
import { DbModule } from "@simulacrum/db";

@Module({
  imports: [DbModule],
  controllers: [],
  providers: [HostctlService, EventsGateway],
})
export class HostctlModule {}
