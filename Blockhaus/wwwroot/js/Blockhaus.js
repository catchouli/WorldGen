/// Blockhaus
class Blockhaus {
  /// The user input state
  userInputs = {
    'forward': false,
    'backward': false,
    'left': false,
    'right': false,
    'up': false,
    'down': false
  }

  /// Constructor
  constructor() {
    // Initialise three.js
    THREE.Cache.enabled = true;
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);

    this.renderer = new THREE.WebGLRenderer();
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    document.body.appendChild(this.renderer.domElement);

    // Set up pointer lock controls
    this.controls = new THREE.PointerLockControls(this.camera, this.renderer.domElement);
    this.scene.add(this.controls.getObject());

    document.addEventListener('keydown', (event) => this.onKeyDown(event));
    document.addEventListener('keyup', (event) => this.onKeyUp(event));
    document.addEventListener('click', () => { this.controls.lock(); });

    // Create lights
    const ambientLight = new THREE.AmbientLight(0x404040);
    this.scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xCCCCCC, 1.0);
    directionalLight.target.position.x = 4.0;
    directionalLight.target.position.y = -10.0;
    directionalLight.target.position.z = -4.0;
    this.scene.add(directionalLight);
    this.scene.add(directionalLight.target);

    // Initialise sky
    this.initSky();

    // Load model
    // TODO: it currently loads the textures multiple times which isn't optimal
    const gltfLoader = new THREE.GLTFLoader();
    for (let z = 0; z < 50; ++z) {
      for (let x = 0; x < 50; ++x) {
        let self = this;
        gltfLoader.load(`api/models/chunk/${x}/0/${z}`, function (obj) {
          // TODO: use env map instead of this
          obj.scene.traverse(child => {
            if (child.material) child.material.metalness = 0;
          });

          // Position them in the right place
          obj.scene.position.x = x * 16;
          obj.scene.position.z = z * 16;

          // TODO: is it efficient to use obj.scene or should we use obj.asset directly?
          self.scene.add(obj.scene);
          self.chunk = obj.scene;
          self.render();
        }, this.xhrProgress, console.error);
      }
    }

    // Set initial camera position and rotation
    this.camera.position.x = -27.5;
    this.camera.position.y = 36.9;
    this.camera.position.z = 4.82;

    this.camera.rotation.x = -2.642;
    this.camera.rotation.y = -0.913;
    this.camera.rotation.z = -2.73;
  }

  /// Initialise sky
  initSky() {
    // Add Sky
    let sky = new THREE.Sky();
    sky.scale.setScalar(450000);
    this.scene.add(sky);

    let sun = new THREE.Vector3();

    /// GUI

    const effectController = {
      turbidity: 10,
      rayleigh: 3,
      mieCoefficient: 0.005,
      mieDirectionalG: 0.7,
      elevation: 2,
      azimuth: 180,
      exposure: this.renderer.toneMappingExposure
    };

    let self = this;
    function guiChanged() {
      const uniforms = sky.material.uniforms;
      uniforms['turbidity'].value = effectController.turbidity;
      uniforms['rayleigh'].value = effectController.rayleigh;
      uniforms['mieCoefficient'].value = effectController.mieCoefficient;
      uniforms['mieDirectionalG'].value = effectController.mieDirectionalG;

      const phi = THREE.MathUtils.degToRad(90 - effectController.elevation);
      const theta = THREE.MathUtils.degToRad(effectController.azimuth);

      sun.setFromSphericalCoords(1, phi, theta);

      uniforms['sunPosition'].value.copy(sun);

      self.renderer.toneMappingExposure = effectController.exposure;
      self.renderer.render(self.scene, self.camera);
    }

    guiChanged();
  }

  update() {
    const moveSpeed = 0.1;

    if (this.cube) {
      this.cube.rotation.y += 0.01;
    }

    if (this.userInputs['forward'])
      this.controls.moveForward(1.0 * moveSpeed);
    if (this.userInputs['backward'])
      this.controls.moveForward(-1.0 * moveSpeed);
    if (this.userInputs['left'])
      this.controls.moveRight(-1.0 * moveSpeed);
    if (this.userInputs['right'])
      this.controls.moveRight(1.0 * moveSpeed);
    if (this.userInputs['up'])
      this.camera.translateY(1.0 * moveSpeed);
    if (this.userInputs['down'])
      this.camera.translateY(-1.0 * moveSpeed);
  }

  render() {
    this.renderer.render(this.scene, this.camera);
  }

  /// Key down handler
  onKeyDown(event) {
    switch (event.code) {
      case 'ArrowUp':
      case 'KeyW':
        this.userInputs['forward'] = true;
        break;
      case 'ArrowLeft':
      case 'KeyA':
        this.userInputs['left'] = true;
        break;
      case 'ArrowDown':
      case 'KeyS':
        this.userInputs['backward'] = true;
        break;
      case 'ArrowRight':
      case 'KeyD':
        this.userInputs['right'] = true;
        break;
      case 'KeyE':
        this.userInputs['up'] = true;
        break;
      case 'KeyQ':
        this.userInputs['down'] = true;
        break;
    }
  }

  /// Key up handler
  onKeyUp(event) {
    switch (event.code) {
      case 'ArrowUp':
      case 'KeyW':
        this.userInputs['forward'] = false;
        break;
      case 'ArrowLeft':
      case 'KeyA':
        this.userInputs['left'] = false;
        break;
      case 'ArrowDown':
      case 'KeyS':
        this.userInputs['backward'] = false;
        break;
      case 'ArrowRight':
      case 'KeyD':
        this.userInputs['right'] = false;
        break;
      case 'KeyE':
        this.userInputs['up'] = false;
        break;
      case 'KeyQ':
        this.userInputs['down'] = false;
        break;
    }
  }

  xhrProgress(xhr) {
    console.log((xhr.loaded / xhr.total * 100) + '% loaded');
  }
}
