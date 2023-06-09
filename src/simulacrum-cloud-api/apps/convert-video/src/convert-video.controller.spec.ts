import { Test, TestingModule } from '@nestjs/testing';
import { ConvertVideoController } from './convert-video.controller';
import { ConvertVideoService } from './convert-video.service';

describe('ConvertVideoController', () => {
  let convertVideoController: ConvertVideoController;

  beforeEach(async () => {
    const app: TestingModule = await Test.createTestingModule({
      controllers: [ConvertVideoController],
      providers: [ConvertVideoService],
    }).compile();

    convertVideoController = app.get<ConvertVideoController>(ConvertVideoController);
  });

  describe('root', () => {
    it('should return "Hello World!"', () => {
      expect(convertVideoController.getHello()).toBe('Hello World!');
    });
  });
});
