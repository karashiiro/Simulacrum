const nodeExternals = require("webpack-node-externals");

module.exports = function (options) {
  return {
    ...options,
    externals: [nodeExternals({ modulesDir: "../../node_modules" })],
  };
};
