import { Module } from '@nestjs/common';
import { HostctlService } from './hostctl.service';
import { EventsGateway } from './events/events.gateway';

@Module({
  imports: [],
  controllers: [],
  providers: [HostctlService, EventsGateway],
})
export class HostctlModule {}
