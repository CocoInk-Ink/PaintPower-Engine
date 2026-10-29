// backend/foreign/F/xPaint.js

const cfs = require("../../FileSystem");

const path = require("path");
const fs = require("fs");
const crypto = require("crypto");
const F = require("../F");
const multer = require("multer");

const os = { path: path } // Use as a fallback. I got used to python's "os.path" so use both

const tools = {
    generateRandomString: function (myLength) {
        const chars =
            "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890";
        const randomArray = Array.from(
            { length: myLength },
            (v, k) => chars[Math.floor(Math.random() * chars.length)]
        );

        const randomString = randomArray.join("");
        return randomString;
    },
    os: os, // Use as a fallback. I got used to python's "os.path" so use both
    cleanFileName: function (name = "") {
        const cleaned = String(name).replace(/[^A-Za-z0-9_-]/g, "_");
        return cleaned || "file";
    }
}

const xPaintProjectImport = multer({
    storage: multer.diskStorage({
        destination: (req, file, cb) => {
            fs.mkdir(cfs.xPaintProjectDir, { recursive: true }, (err) => cb(err, cfs.xPaintProjectDir));
        },
        filename: (req, file, cb) => {
            cb(null, Date.now() + "-" + tools.generateRandomString(16));
        }
    }),
    limits: {
        fileSize: 1024 * 1024 * 300,
        files: 2
    }
});

const xPaintVersionImport = xPaintProjectImport.fields([
    { name: "source", maxCount: 1 },
    { name: "build", maxCount: 1 },
    { name: "file", maxCount: 1 }
]);

class xPaint extends F.Foreign {
    constructor() {
        super("xPaint");
        this.versionSchemaReady = false;
        this.legacyMigrations = new Map();

        this.schema = `
            CREATE TABLE IF NOT EXISTS projects (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                notes TEXT,
                ownerid INTEGER NOT NULL,
                creators_ids INTEGER[] NOT NULL,
                isShared INTEGER DEFAULT 0,
                canDownload INTEGER DEFAULT 0,
                allowComments INTEGER DEFAULT 1,
                fileInfo TEXT,
                -- JSON string {name,type,size,uploadedAt}

                newestVersion INTEGER,
                selectedVersion INTEGER,

                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                views INTEGER DEFAULT 0,
                likes INTEGER DEFAULT 0,
                favorites INTEGER DEFAULT 0,
                path TEXT
            );

            CREATE TABLE IF NOT EXISTS project_versions (
                id TEXT PRIMARY KEY,
                projectid INTEGER NOT NULL,
                version TEXT,
                source TEXT NOT NULL,
                build TEXT,
                sourceInfo TEXT,
                buildInfo TEXT,
                isShared INTEGER NOT NULL DEFAULT 0,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (projectid) REFERENCES projects(id)
            );

            CREATE INDEX IF NOT EXISTS project_versions_projectid
                ON project_versions(projectid, created_at);
        `;

        this.makeDBAndAttach("projects", true);
        this.bindFunctions();
        this.bindRoutes();
        this.initDB().catch((err) => console.error("xPaint database initialization failed:", err));
    }

    async initDB() {
        await super.initDB();
        if (this.versionSchemaReady) return;

        const db = this.db.getDb();
        const columns = new Set();
        const stmt = db.prepare("PRAGMA table_info(projects)");
        while (stmt.step()) columns.add(stmt.getAsObject().name);
        stmt.free();

        for (const [name, definition] of [
            ["newestVersion", "TEXT"],
            ["selectedVersion", "TEXT"],
            ["path", "TEXT"]
        ]) {
            if (!columns.has(name)) db.run(`ALTER TABLE projects ADD COLUMN ${name} ${definition}`);
        }

        this.versionSchemaReady = true;
    }

    bindFunctions() {
        this.bindDBFunc("loadProject", (id) => {
            const db = this.db.getDb();
            const stmt = db.prepare("SELECT * FROM projects WHERE id = ?");
            stmt.bind([id]);
            if (!stmt.step()) {
                stmt.free();
                return null;
            }
            const project = stmt.getAsObject();
            stmt.free();
            return project;
        });

        this.bindDBFunc("countProjects", () => {
            const db = this.db.getDb();
            const stmt = db.prepare("SELECT COUNT(*) AS c FROM projects");
            stmt.step();
            const row = stmt.getAsObject();
            stmt.free();
            return row.c;
        });

        this.bindDBFunc("getOwner", (id) => {
            const db = this.db.getDb();
            const stmt = db.prepare("SELECT ownerid FROM projects WHERE id = ?");
            stmt.bind([id]);

            if (!stmt.step()) {
                stmt.free();
                return null;
            }

            const row = stmt.getAsObject();
            stmt.free();
            return row.ownerid;
        });

        this.bindDBFunc("getProject", (id) => {
            const db = this.db.getDb();
            const stmt = db.prepare("SELECT * FROM projects WHERE id = ?");
            stmt.bind([id]);
            if (!stmt.step()) {
                stmt.free();
                return null;
            }
            const project = stmt.getAsObject();
            stmt.free();
            return project;
        });

        this.bindDBFunc("createProject", (data) => {
            const db = this.db.getDb();

            const stmt = db.prepare(`
                INSERT INTO projects
                    (title, description, notes, ownerid, creators_ids,
                    isShared, canDownload, allowComments, fileInfo, path)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            `);



            stmt.run([
                data.title,
                data.description,
                data.notes,
                data.ownerid,
                JSON.stringify(data.creators_ids || []),
                data.isShared ? 1 : 0,
                data.canDownload ? 1 : 0,
                data.allowComments ? 1 : 0,
                JSON.stringify(data.fileInfo || {}),
                data.path || ""
            ]);

            stmt.free();

            // get last inserted ID
            const id = db.db.prepare("SELECT last_insert_rowid() AS id").get().id;
            return id;
        });

        this.bindDBFunc("createProjectVersion", (data) => {
            const db = this.db.getDb();
            const insert = db.db.prepare(`
                INSERT INTO project_versions
                    (id, projectid, version, source, build, sourceInfo, buildInfo, isShared)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            `);
            const updateProject = db.db.prepare(`
                UPDATE projects
                SET newestVersion = ?, selectedVersion = COALESCE(selectedVersion, ?),
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = ?
            `);

            db.db.transaction(() => {
                insert.run(
                    data.id,
                    data.projectid,
                    data.version || null,
                    data.source,
                    data.build || null,
                    JSON.stringify(data.sourceInfo),
                    data.buildInfo ? JSON.stringify(data.buildInfo) : null,
                    data.isShared ? 1 : 0
                );
                updateProject.run(data.id, data.id, data.projectid);
            })();

            return data.id;
        });

        this.bindDBFunc("getProjectVersion", (projectid, projectVersion) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                SELECT * FROM project_versions
                WHERE projectid = ? AND id = ?
            `);

            stmt.bind([projectid, projectVersion]);

            if (!stmt.step()) {
                stmt.free();
                return null;
            }

            const row = stmt.getAsObject();
            stmt.free();
            return row;
        });

        this.bindDBFunc("listProjectVersions", (projectid) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                SELECT * FROM project_versions
                WHERE projectid = ?
                ORDER BY created_at DESC, rowid DESC
            `);
            stmt.bind([projectid]);
            const versions = [];
            while (stmt.step()) versions.push(stmt.getAsObject());
            stmt.free();
            return versions;
        });

        this.bindDBFunc("getLatestProjectVersion", (projectid) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                SELECT * FROM project_versions
                WHERE projectid = ?
                ORDER BY created_at DESC, rowid DESC
                LIMIT 1
            `);
            stmt.bind([projectid]);
            if (!stmt.step()) {
                stmt.free();
                return null;
            }
            const version = stmt.getAsObject();
            stmt.free();
            return version;
        });

        this.bindDBFunc("selectProjectVersion", (projectid, versionId) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                UPDATE projects
                SET selectedVersion = ?, updated_at = CURRENT_TIMESTAMP
                WHERE id = ?
            `);
            stmt.run([versionId, projectid]);
            stmt.free();
        });

        this.bindDBFunc("updateProjectVersionSource", (projectid, versionId, sourceInfo) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                UPDATE project_versions
                SET source = ?, sourceInfo = ?
                WHERE projectid = ? AND id = ?
            `);
            stmt.run(["source.xpaint", JSON.stringify(sourceInfo), projectid, versionId]);
            stmt.free();
        });

        this.bindDBFunc("updateProjectVersionSharing", (projectid, versionId, isShared) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                UPDATE project_versions SET isShared = ?
                WHERE projectid = ? AND id = ?
            `);
            stmt.run([isShared ? 1 : 0, projectid, versionId]);
            stmt.free();
        });

        this.bindDBFunc("updateProjectVersionBuild", (projectid, versionId, buildInfo) => {
            const db = this.db.getDb();
            const stmt = db.prepare(`
                UPDATE project_versions
                SET build = ?, buildInfo = ?
                WHERE projectid = ? AND id = ?
            `);
            stmt.run(["build.xpe", JSON.stringify(buildInfo), projectid, versionId]);
            stmt.free();
        });

        this.bindDBFunc("deleteProjectVersion", (projectid, versionId) => {
            const db = this.db.getDb();
            const remove = db.db.prepare("DELETE FROM project_versions WHERE projectid = ? AND id = ?");
            const update = db.db.prepare(`
                UPDATE projects
                SET newestVersion = (
                    SELECT id FROM project_versions
                    WHERE projectid = ? ORDER BY created_at DESC, rowid DESC LIMIT 1
                ),
                selectedVersion = CASE WHEN selectedVersion = ? THEN (
                    SELECT id FROM project_versions
                    WHERE projectid = ? ORDER BY created_at DESC, rowid DESC LIMIT 1
                ) ELSE selectedVersion END,
                updated_at = CURRENT_TIMESTAMP
                WHERE id = ?
            `);
            db.db.transaction(() => {
                remove.run(projectid, versionId);
                update.run(projectid, versionId, projectid, projectid);
            })();
        });

        this.bindDBFunc("deleteProject", (id) => {
            const db = this.db.getDb();
            const removeVersions = db.db.prepare("DELETE FROM project_versions WHERE projectid = ?");
            const removeProject = db.db.prepare("DELETE FROM projects WHERE id = ?");
            db.db.transaction(() => {
                removeVersions.run(id);
                removeProject.run(id);
            })();
        });

        this.bindDBFunc("updateProject", (id, data) => {
            const db = this.db.getDb();

            const stmt = db.prepare(`
                UPDATE projects SET
                    title = ?,
                    description = ?,
                    notes = ?,
                    creators_ids = ?,
                    isShared = ?,
                    canDownload = ?,
                    allowComments = ?,
                    fileInfo = ?,
                    path = ?,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = ?
            `);

            stmt.run([
                data.title,
                data.description,
                data.notes,
                JSON.stringify(data.creators_ids || []),
                data.isShared ? 1 : 0,
                data.canDownload ? 1 : 0,
                data.allowComments ? 1 : 0,
                JSON.stringify(data.fileInfo || {}),
                data.path || "",
                id
            ]);

            stmt.free();
        });

    }

    parseFileInfo(value) {
        if (!value) return null;
        try {
            return JSON.parse(value);
        } catch {
            return null;
        }
    }

    versionResponse(project, version) {
        return {
            id: version.id,
            projectId: String(project.id),
            version: version.version,
            isShared: Number(version.isShared) === 1,
            source: this.parseFileInfo(version.sourceInfo),
            build: this.parseFileInfo(version.buildInfo),
            createdAt: version.created_at,
            sourceUrl: `/api/projects/${project.id}/versions/${version.id}/source`,
            buildUrl: version.build
                ? `/api/projects/${project.id}/versions/${version.id}/build`
                : null
        };
    }

    async authorizeProject(req, res, projectId, requireOwner = false) {
        await this.initDB();
        const project = this.callDBFunc("getProject", projectId);

        if (!project) {
            res.status(404).json({ status: "Project not found" });
            return null;
        }

        const userId = req.session?.userId;
        const isAdmin = require('../../db.js').getUserById(userId)?.role === "Admin";

        console.log(isAdmin);
        console.log(require('../../db.js').getUserById(userId)?.role);

        let creators = [];
        try {
            creators = Array.isArray(project.creators_ids)
                ? project.creators_ids
                : JSON.parse(project.creators_ids || "[]");
        } catch {
            creators = [];
        }

        const isOwner = userId != null && String(project.ownerid) === String(userId);
        const isCreator = userId != null &&
            Array.isArray(creators) &&
            creators.some((creatorId) => String(creatorId) === String(userId));

        if (!isAdmin && (requireOwner && !isOwner)) {
            res.status(userId ? 403 : 401).json({ status: userId ? "Forbidden" : "Not logged in" });
            return null;
        }

        if (!isAdmin && (!isOwner && !isCreator && Number(project.isShared) !== 1)) {
            res.status(userId ? 403 : 401).json({ status: "Access denied" });
            return null;
        }

        return { project, isOwner, isCreator, isAdmin };
    }

    async authorizeVersion(req, res, projectId, versionId, requireOwner = false) {
        const access = await this.authorizeProject(req, res, projectId, requireOwner);
        if (!access) return null;

        const version = this.callDBFunc("getProjectVersion", access.project.id, versionId);
        if (!version) {
            res.status(404).json({ status: "Version not found" });
            return null;
        }

        if (!access.isAdmin && (!access.isOwner && !access.isCreator && Number(version.isShared) !== 1)) {
            res.status(403).json({ status: "Version is not shared" });
            return null;
        }

        return { ...access, version };
    }

    versionDirectory(projectId, versionId) {
        if (!/^[a-f0-9]{32}$/.test(versionId)) return null;
        return path.join(cfs.xPaintProjectDir, String(projectId), "versions", versionId);
    }

    async saveVersion(project, sourceFile, buildFile, versionName, isShared = false) {
        if (!sourceFile || !/\.xpaint$/i.test(sourceFile.originalname)) {
            throw new Error("A .xpaint source file is required");
        }
        if (buildFile && !/\.xpe$/i.test(buildFile.originalname)) {
            throw new Error("Build file must use the .xpe extension");
        }

        const id = crypto.randomBytes(16).toString("hex");
        const directory = this.versionDirectory(project.id, id);
        const sourceInfo = {
            name: sourceFile.originalname,
            type: sourceFile.mimetype,
            size: sourceFile.size,
            uploadedAt: new Date().toISOString()
        };
        const buildInfo = buildFile ? {
            name: buildFile.originalname,
            type: buildFile.mimetype,
            size: buildFile.size,
            uploadedAt: new Date().toISOString()
        } : null;

        await fs.promises.mkdir(directory, { recursive: true });

        try {
            await fs.promises.rename(sourceFile.path, path.join(directory, "source.xpaint"));
            if (buildFile) {
                await fs.promises.rename(buildFile.path, path.join(directory, "build.xpe"));
            }

            this.callDBFunc("createProjectVersion", {
                id,
                projectid: project.id,
                version: versionName || null,
                source: "source.xpaint",
                build: buildFile ? "build.xpe" : null,
                sourceInfo,
                buildInfo,
                isShared
            });
        } catch (err) {
            try {
                await fs.promises.rm(directory, { recursive: true, force: true });
                for (const file of [sourceFile, buildFile]) {
                    if (file && fs.existsSync(file.path)) {
                        await fs.promises.rm(file.path, { force: true });
                    }
                }
            } catch (cleanupError) {
                console.error("xPaint failed to clean up an incomplete version:", cleanupError);
            }
            throw err;
        }

        return {
            id,
            projectId: String(project.id),
            version: versionName || null,
            isShared: Boolean(isShared),
            source: sourceInfo,
            build: buildInfo,
            createdAt: new Date().toISOString(),
            sourceUrl: `/api/projects/${project.id}/versions/${id}/source`,
            buildUrl: buildFile ? `/api/projects/${project.id}/versions/${id}/build` : null
        };
    }

    sendVersionFile(res, project, version, kind) {
        const directory = this.versionDirectory(project.id, version.id);
        if (!directory) return res.status(404).json({ status: "File not found" });

        const extension = kind === "source" ? "xpaint" : "xpe";
        const metadata = this.parseFileInfo(kind === "source" ? version.sourceInfo : version.buildInfo);
        const storedName = kind === "source" ? version.source : version.build;
        if (storedName !== `${kind}.${extension}`) {
            return res.status(404).json({ status: "File not found" });
        }

        const filePath = path.join(directory, storedName);
        if (!fs.existsSync(filePath)) return res.status(404).json({ status: "File not found" });

        res.setHeader("Content-Type", "application/octet-stream");
        res.setHeader(
            "Content-Disposition",
            `attachment; filename="${tools.cleanFileName(path.parse(metadata?.name || project.title).name)}.${extension}"`
        );
        const stream = fs.createReadStream(filePath);
        stream.on("error", (err) => {
            console.error("xPaint version file stream failed:", err);
            if (!res.headersSent) res.status(500).end();
            else res.destroy(err);
        });
        return stream.pipe(res);
    }

    async discardUploads(files = {}) {
        const uploaded = Object.values(files).flat();
        await Promise.all(uploaded.map((file) => fs.promises.rm(file.path, { force: true })));
    }

    async replaceVersionFile(project, version, kind, file) {
        const extension = kind === "source" ? ".xpaint" : ".xpe";
        if (!file || !path.extname(file.originalname).toLowerCase().endsWith(extension)) {
            throw new Error(`The ${kind} file must use the ${extension} extension`);
        }

        const directory = this.versionDirectory(project.id, version.id);
        if (!directory) throw new Error("Invalid version");
        await fs.promises.mkdir(directory, { recursive: true });

        const fileInfo = {
            name: file.originalname,
            type: file.mimetype,
            size: file.size,
            uploadedAt: new Date().toISOString()
        };
        const storedName = kind === "source" ? "source.xpaint" : "build.xpe";
        const target = path.join(directory, storedName);
        const temporary = path.join(directory, `${storedName}.${crypto.randomBytes(8).toString("hex")}.tmp`);
        const backup = path.join(directory, `${storedName}.${crypto.randomBytes(8).toString("hex")}.bak`);
        let backedUp = false;

        try {
            await fs.promises.copyFile(file.path, temporary);
            if (fs.existsSync(target)) {
                await fs.promises.rename(target, backup);
                backedUp = true;
            }
            await fs.promises.rename(temporary, target);

            if (kind === "source") {
                this.callDBFunc("updateProjectVersionSource", project.id, version.id, fileInfo);
            } else {
                this.callDBFunc("updateProjectVersionBuild", project.id, version.id, fileInfo);
            }

            if (backedUp) {
                try {
                    await fs.promises.rm(backup, { force: true });
                } catch (cleanupError) {
                    console.error("xPaint could not remove a replaced version file backup:", cleanupError);
                }
            }
        } catch (err) {
            try {
                await fs.promises.rm(temporary, { force: true });
                if (backedUp) {
                    await fs.promises.rm(target, { force: true });
                    await fs.promises.rename(backup, target);
                } else {
                    await fs.promises.rm(target, { force: true });
                }
            } catch (cleanupError) {
                console.error("xPaint failed to restore a replaced version file:", cleanupError);
            }
            throw err;
        } finally {
            try {
                await fs.promises.rm(file.path, { force: true });
            } catch (cleanupError) {
                console.error("xPaint failed to clean up a version file upload:", cleanupError);
            }
        }

        const updatedVersion = this.callDBFunc("getProjectVersion", project.id, version.id);
        return this.versionResponse(project, updatedVersion);
    }

    async sendLatestVersionFile(req, res, kind) {
        const access = await this.authorizeProject(req, res, req.params.id);
        if (!access) return;

        const migratedVersion = await this.ensureLegacyVersion(access.project);
        const version = access.project.newestVersion
            ? this.callDBFunc("getProjectVersion", access.project.id, access.project.newestVersion)
            : migratedVersion;
        if (!version) return res.status(404).json({ status: "Version not found" });
        if (!access.isAdmin && (!access.isOwner && !access.isCreator && Number(version.isShared) !== 1)) {
            return res.status(403).json({ status: "Version is not shared" });
        }

        return this.sendVersionFile(res, access.project, version, kind);
    }

    async handleVersionFileUpdate(req, res, kind) {
        const file = req.file;
        if (!file) return res.status(400).json({ status: "No file uploaded" });

        try {
            const extension = kind === "source" ? ".xpaint" : ".xpe";
            if (path.extname(file.originalname).toLowerCase() !== extension) {
                await fs.promises.rm(file.path, { force: true });
                return res.status(400).json({ status: `File must use the ${extension} extension` });
            }
            if (!req.session?.userId) {
                await fs.promises.rm(file.path, { force: true });
                return res.status(401).json({ status: "Not logged in" });
            }

            const access = await this.authorizeVersion(
                req,
                res,
                req.params.id,
                req.params.versionId,
                true
            );
            if (!access) {
                await fs.promises.rm(file.path, { force: true });
                return;
            }

            const version = await this.replaceVersionFile(
                access.project,
                access.version,
                kind,
                file
            );
            return res.json({ status: "ok", version });
        } catch (err) {
            console.error(`xPaint version ${kind} update failed:`, err);
            try {
                await fs.promises.rm(file.path, { force: true });
            } catch (cleanupError) {
                console.error("xPaint failed to clean up an uploaded version file:", cleanupError);
            }
            if (!res.headersSent) return res.status(500).json({ status: "Failed", error: err.message });
        }
    }

    async ensureLegacyVersion(project) {
        const current = this.callDBFunc("getLatestProjectVersion", project.id);
        if (current) return current;

        const legacyFile = path.join(
            cfs.xPaintProjectDir,
            String(project.id),
            "project.xPaint"
        );
        if (!fs.existsSync(legacyFile)) return null;

        const projectKey = String(project.id);
        if (this.legacyMigrations.has(projectKey)) {
            await this.legacyMigrations.get(projectKey);
            return this.callDBFunc("getLatestProjectVersion", project.id);
        }

        const migration = (async () => {
            const id = crypto.randomBytes(16).toString("hex");
            const directory = this.versionDirectory(project.id, id);
            const stat = await fs.promises.stat(legacyFile);
            const sourceInfo = {
                name: "project.xPaint",
                type: "application/octet-stream",
                size: stat.size,
                uploadedAt: stat.mtime.toISOString()
            };

            await fs.promises.mkdir(directory, { recursive: true });
            try {
                await fs.promises.copyFile(legacyFile, path.join(directory, "source.xpaint"));
                this.callDBFunc("createProjectVersion", {
                    id,
                    projectid: project.id,
                    version: "Legacy version",
                    source: "source.xpaint",
                    build: null,
                    sourceInfo,
                    buildInfo: null,
                    isShared: Number(project.isShared) === 1
                });
            } catch (err) {
                try {
                    await fs.promises.rm(directory, { recursive: true, force: true });
                } catch (cleanupError) {
                    console.error("xPaint failed to clean up a legacy version migration:", cleanupError);
                }
                throw err;
            }
        })();

        this.legacyMigrations.set(projectKey, migration);
        try {
            await migration;
        } finally {
            this.legacyMigrations.delete(projectKey);
        }

        return this.callDBFunc("getLatestProjectVersion", project.id);
    }

    async uploadLegacySource(req, res, createProject) {
        const file = req.file;
        if (!file) return res.status(400).json({ status: "No project uploaded." });

        let project = null;
        let createdProjectId = null;
        try {
            if (!req.session?.userId) {
                await fs.promises.rm(file.path, { force: true });
                return res.status(401).json({ status: "Not logged in" });
            }
            if (!/\.xpaint$/i.test(file.originalname)) {
                await fs.promises.rm(file.path, { force: true });
                return res.status(400).json({ status: "Source file must use the .xpaint extension" });
            }

            await this.initDB();
            if (createProject) {
                createdProjectId = this.callDBFunc("createProject", {
                    title: req.body.title || "Untitled",
                    description: req.body.description || null,
                    notes: req.body.notes || null,
                    ownerid: req.session.userId,
                    creators_ids: [req.session.userId],
                    isShared: false,
                    canDownload: false,
                    allowComments: true,
                    fileInfo: {},
                    path: ""
                });
                project = this.callDBFunc("getProject", createdProjectId);
            } else {
                const access = await this.authorizeProject(req, res, req.params.id, true);
                if (!access) {
                    await fs.promises.rm(file.path, { force: true });
                    return;
                }
                project = access.project;
            }

            const version = await this.saveVersion(project, file, null, req.body.version);
            return res.json({
                status: "ok",
                id: String(project.id),
                version,
                ...(createProject ? {} : { replaced: true })
            });
        } catch (err) {
            console.error("xPaint source upload failed:", err);
            try {
                if (fs.existsSync(file.path)) await fs.promises.rm(file.path, { force: true });
            } catch (cleanupError) {
                console.error("xPaint failed to clean up a source upload:", cleanupError);
            }
            if (createdProjectId !== null) {
                try {
                    this.callDBFunc("deleteProject", createdProjectId);
                    await fs.promises.rm(
                        path.join(cfs.xPaintProjectDir, String(createdProjectId)),
                        { recursive: true, force: true }
                    );
                } catch (cleanupError) {
                    console.error("xPaint failed to clean up a newly created project:", cleanupError);
                }
            }
            if (!res.headersSent) return res.status(500).json({ status: "Failed", error: err.message });
        }
    }

    bindRoutes() {
        // whoami
        this.route("get", "/api/whoami", (req, res) => {
            if (!req.session.userId)
                return res.status(401).send("Not logged in");

            res.json({ id: req.session.userId });
        });

        this.route("get", "/api/servercheck", (req, res) => {
            return res.status(200).send("Ok.");
        });

        this.route("get", "/api/load/projects/:id", async (req, res) => {
            try {
                const access = await this.authorizeProject(req, res, req.params.id);
                if (!access) return;
                const project = this.callDBFunc("loadProject", access.project.id);
                res.json({ status: "ok", project });
            } catch (err) {
                console.error("xPaint project retrieval failed:", err);
                res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("get", "/api/projects/:id/metadata", async (req, res) => {
            try {
                const access = await this.authorizeProject(req, res, req.params.id);
                if (!access) return;
                res.json(access.project);
            } catch (err) {
                console.error("xPaint project metadata retrieval failed:", err);
                res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        // Get the set version of a project
        this.route("get", "/api/projects/:id/file/selected/source", async (req, res) => {
            try {
                const projectAccess = await this.authorizeProject(req, res, req.params.id);
                if (!projectAccess) return;
                const versionId = projectAccess.project.selectedVersion;
                if (!versionId) return res.status(404).json({ status: "No selected version" });

                const access = await this.authorizeVersion(
                    req, res, projectAccess.project.id, versionId
                );
                if (!access) return;
                return this.sendVersionFile(res, access.project, access.version, "source");
            } catch (err) {
                console.error("xPaint selected source stream failed:", err);
                if (!res.headersSent) res.status(500).json({ status: "Failed" });
            }
        });

        this.route("get", "/api/projects/:id/file/selected/build", async (req, res) => {
            try {
                const projectAccess = await this.authorizeProject(req, res, req.params.id);
                if (!projectAccess) return;
                const versionId = projectAccess.project.selectedVersion;
                if (!versionId) return res.status(404).json({ status: "No selected version" });
                const access = await this.authorizeVersion(
                    req, res, projectAccess.project.id, versionId
                );
                if (!access) return;
                if (!access.version.build) return res.status(404).json({ status: "Selected version has no build" });
                return this.sendVersionFile(res, access.project, access.version, "build");
            } catch (err) {
                console.error("xPaint selected build stream failed:", err);
                if (!res.headersSent) res.status(500).json({ status: "Failed" });
            }
        });

        // Get the latest version of a project
        this.route("get", "/api/projects/:id/file/latest/source", async (req, res) => {
            try {
                return await this.sendLatestVersionFile(req, res, "source");
            } catch (err) {
                console.error("xPaint source stream failed:", err);
                if (!res.headersSent) res.status(500).json({ status: "Failed" });
            }
        });

        this.route("get", "/api/projects/:id/file/latest/build", async (req, res) => {
            try {
                return await this.sendLatestVersionFile(req, res, "build");
            } catch (err) {
                console.error("xPaint build stream failed:", err);
                if (!res.headersSent) res.status(500).json({ status: "Failed" });
            }
        });

        this.route("post", "/api/projects/:id/versions", xPaintVersionImport, async (req, res) => {
            const files = req.files || {};
            let sourceFile = files.source?.[0];
            let buildFile = files.build?.[0];
            const fileAlias = files.file?.[0];
            try {
                if (fileAlias) {
                    if (/\.xpaint$/i.test(fileAlias.originalname) && !sourceFile) {
                        sourceFile = fileAlias;
                    } else if (/\.xpe$/i.test(fileAlias.originalname) && !buildFile) {
                        buildFile = fileAlias;
                    } else {
                        await this.discardUploads(files);
                        return res.status(400).json({
                            status: "file must be a .xpaint source or .xpe build"
                        });
                    }
                }

                if (buildFile && !sourceFile) {
                    if (!/\.xpe$/i.test(buildFile.originalname)) {
                        await this.discardUploads(files);
                        return res.status(400).json({ status: "Build file must use the .xpe extension" });
                    }
                    if (!req.session?.userId) {
                        await this.discardUploads(files);
                        return res.status(401).json({ status: "Not logged in" });
                    }

                    const access = await this.authorizeProject(req, res, req.params.id, true);
                    if (!access) {
                        await this.discardUploads(files);
                        return;
                    }

                    const latestVersion = this.callDBFunc(
                        "getLatestProjectVersion",
                        access.project.id
                    );
                    if (!latestVersion) {
                        await this.discardUploads(files);
                        return res.status(400).json({
                            status: "Upload a source version before uploading a build"
                        });
                    }

                    const version = await this.saveBuildToVersion(
                        access.project,
                        latestVersion,
                        buildFile
                    );
                    await this.discardUploads(files);
                    return res.json({ status: "ok", version });
                }

                if (!sourceFile) {
                    await this.discardUploads(files);
                    return res.status(400).json({ status: "A .xpaint source file is required" });
                }
                if (!/\.xpaint$/i.test(sourceFile.originalname) ||
                    (buildFile && !/\.xpe$/i.test(buildFile.originalname))) {
                    await this.discardUploads(files);
                    return res.status(400).json({
                        status: "Source files must use .xpaint and build files must use .xpe"
                    });
                }
                if (!req.session?.userId) {
                    await this.discardUploads(files);
                    return res.status(401).json({ status: "Not logged in" });
                }

                const access = await this.authorizeProject(req, res, req.params.id, true);
                if (!access) {
                    await this.discardUploads(files);
                    return;
                }

                const version = await this.saveVersion(
                    access.project,
                    sourceFile,
                    buildFile,
                    req.body.version,
                    req.body.isShared === true || req.body.isShared === "1" || req.body.isShared === "true"
                );
                return res.status(201).json({ status: "ok", version });
            } catch (err) {
                console.error("xPaint version creation failed:", err);
                try {
                    await this.discardUploads(files);
                } catch (cleanupError) {
                    console.error("xPaint failed to clean up version uploads:", cleanupError);
                }
                if (!res.headersSent) return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("get", "/api/projects/:id/versions", async (req, res) => {
            try {
                const access = await this.authorizeProject(req, res, req.params.id);
                if (!access) return;
                await this.ensureLegacyVersion(access.project);
                const versions = this.callDBFunc("listProjectVersions", access.project.id)
                    .filter((version) => access.isAdmin || access.isOwner || access.isCreator || Number(version.isShared) === 1)
                    .map((version) => this.versionResponse(access.project, version));
                return res.json({ status: "ok", versions });
            } catch (err) {
                console.error("xPaint version listing failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("get", "/api/projects/:id/versions/:versionId", async (req, res) => {
            try {
                const access = await this.authorizeVersion(
                    req, res, req.params.id, req.params.versionId
                );
                if (!access) return;
                return res.json({
                    status: "ok",
                    version: this.versionResponse(access.project, access.version)
                });
            } catch (err) {
                console.error("xPaint version retrieval failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("get", "/api/projects/:id/versions/:versionId/source", async (req, res) => {
            try {
                const access = await this.authorizeVersion(
                    req, res, req.params.id, req.params.versionId
                );
                if (!access) return;
                return this.sendVersionFile(res, access.project, access.version, "source");
            } catch (err) {
                console.error("xPaint version source stream failed:", err);
                if (!res.headersSent) res.status(500).json({ status: "Failed" });
            }
        });

        this.route(
            "post",
            "/api/projects/:id/versions/:versionId/source",
            xPaintProjectImport.single("file"),
            (req, res) => this.handleVersionFileUpdate(req, res, "source")
        );

        this.route("get", "/api/projects/:id/versions/:versionId/build", async (req, res) => {
            try {
                const access = await this.authorizeVersion(
                    req, res, req.params.id, req.params.versionId
                );
                if (!access) return;
                if (!access.version.build) return res.status(404).json({ status: "Build not found" });
                return this.sendVersionFile(res, access.project, access.version, "build");
            } catch (err) {
                console.error("xPaint version build stream failed:", err);
                if (!res.headersSent) res.status(500).json({ status: "Failed" });
            }
        });

        this.route(
            "post",
            "/api/projects/:id/versions/:versionId/build",
            xPaintProjectImport.single("file"),
            (req, res) => this.handleVersionFileUpdate(req, res, "build")
        );

        this.route("post", "/api/projects/:id/versions/:versionId/select", async (req, res) => {
            try {
                const access = await this.authorizeVersion(
                    req,
                    res,
                    req.params.id,
                    req.params.versionId,
                    true
                );
                if (!access) return;

                this.callDBFunc(
                    "selectProjectVersion",
                    access.project.id,
                    access.version.id
                );
                return res.json({
                    status: "ok",
                    selectedVersion: access.version.id
                });
            } catch (err) {
                console.error("xPaint version selection failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("patch", "/api/projects/:id/versions/:versionId/share", async (req, res) => {
            try {
                const access = await this.authorizeVersion(
                    req, res, req.params.id, req.params.versionId, true
                );
                if (!access) return;
                const value = req.body?.isShared;
                if (![true, false, 1, 0, "1", "0", "true", "false"].includes(value)) {
                    return res.status(400).json({ status: "isShared must be a boolean" });
                }
                const isShared = value === true || value === 1 || value === "1" || value === "true";
                this.callDBFunc(
                    "updateProjectVersionSharing",
                    access.project.id,
                    access.version.id,
                    isShared
                );
                return res.json({ status: "ok", isShared });
            } catch (err) {
                console.error("xPaint version sharing update failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("delete", "/api/projects/:id/versions/:versionId", async (req, res) => {
            try {
                const access = await this.authorizeVersion(
                    req, res, req.params.id, req.params.versionId, true
                );
                if (!access) return;

                const directory = this.versionDirectory(access.project.id, access.version.id);
                this.callDBFunc("deleteProjectVersion", access.project.id, access.version.id);
                if (directory) await fs.promises.rm(directory, { recursive: true, force: true });
                return res.json({ status: "ok" });
            } catch (err) {
                console.error("xPaint version deletion failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route(
            "post",
            "/api/projects/new/upload/paintfile/",
            xPaintProjectImport.single("file"),
            (req, res) => this.uploadLegacySource(req, res, true)
        );

        this.route(
            "post",
            "/api/projects/:id/upload/paintfile/",
            xPaintProjectImport.single("file"),
            (req, res) => this.uploadLegacySource(req, res, false)
        );

        this.route(
            "post",
            "/api/upload/projects/:id/",
            xPaintProjectImport.single("file"),
            (req, res) => this.uploadLegacySource(req, res, false)
        );

        // count projects
        this.route("get", "/api/count/projects", async (req, res) => {
            await this.initDB();
            res.send(String(this.callDBFunc("countProjects")));
        });


        // list projects
        this.route("get", "/api/listProjects", async (req, res) => {
            try {
                await this.initDB();
                const stmt = this.db.getDb().prepare("SELECT * FROM projects ORDER BY id ASC");
                const projects = [];
                const userId = req.session?.userId;

                while (stmt.step()) {
                    const project = stmt.getAsObject();
                    let creators = [];
                    try {
                        creators = Array.isArray(project.creators_ids)
                            ? project.creators_ids
                            : JSON.parse(project.creators_ids || "[]");
                    } catch {
                        creators = [];
                    }

                    const isOwner = userId != null && String(project.ownerid) === String(userId);
                    const isCreator = userId != null && Array.isArray(creators) && creators.some(
                        (creatorId) => String(creatorId) === String(userId)
                    );
                    if (!isOwner && !isCreator && Number(project.isShared) !== 1) continue;

                    projects.push({
                        id: project.id,
                        title: project.title,
                        updatedAt: project.updated_at,
                        isShared: Number(project.isShared) === 1,
                        newestVersion: project.newestVersion || null
                    });
                }

                stmt.free();
                return res.json(projects);
            } catch (err) {
                console.error("xPaint project listing failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("get", "/upload", async (req, res) => {
            return res.status(200).send(`
                <h2>Upload new</h2> <form id="uploadForm" enctype="multipart/form-data" method="POST" action="/f/xPaint/api/projects/new/upload/paintfile">
                    <input type="file" name="file"><button type="submit">Upload</button>
                </form>

                <h2>Upload source to project 1</h2>
                <form id="uploadForm2" enctype="multipart/form-data" method="POST" action="/f/xPaint/api/projects/1/upload/paintfile">
                    <input type="file" name="file"><button type="submit">Upload to project 1</button>
                </form>

                <h2>Upload build to project 1</h2>
                <form id="uploadForm3" enctype="multipart/form-data" method="POST" action="/f/xPaint/api/projects/1/versions">
                    <input type="file" name="file"><button type="submit">Upload build to project 1</button>
                </form>

                <p id="uploadResult"></p>
            `);
        });

        this.route("post", "/api/projects/create", async (req, res) => {
            const ownerid = req.session?.userId;
            if (!ownerid)
                return res.status(401).json({ status: "Not logged in" });

            try {
                await this.initDB();
                const id = this.callDBFunc("createProject", {
                    title: req.body.title || "Untitled",
                    description: req.body.description || null,
                    notes: req.body.notes || null,
                    ownerid,
                    creators_ids: [ownerid],
                    isShared: false,
                    canDownload: false,
                    allowComments: true,
                    fileInfo: {},
                    path: ""
                });
                return res.json({ status: "ok", id: String(id) });
            } catch (err) {
                console.error("xPaint project creation failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("post", "/api/projects/:id/update", async (req, res) => {
            try {
                const access = await this.authorizeProject(req, res, req.params.id, true);
                if (!access) return;
                let creatorsIds = [];
                if (req.body.creators_ids) {
                    try {
                        creatorsIds = JSON.parse(req.body.creators_ids);
                    } catch {
                        return res.status(400).json({ status: "Invalid creators_ids" });
                    }
                    if (!Array.isArray(creatorsIds)) {
                        return res.status(400).json({ status: "creators_ids must be an array" });
                    }
                }

                this.callDBFunc("updateProject", access.project.id, {
                    title: req.body.title,
                    description: req.body.description,
                    notes: req.body.notes,
                    creators_ids: creatorsIds,
                    isShared: req.body.isShared === "1" || req.body.isShared === true,
                    canDownload: req.body.canDownload === "1" || req.body.canDownload === true,
                    allowComments: req.body.allowComments !== "0" && req.body.allowComments !== false
                });
                return res.json({ status: "ok" });
            } catch (err) {
                console.error("xPaint project update failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("post", "/api/projects/:id/delete", async (req, res) => {
            try {
                const access = await this.authorizeProject(req, res, req.params.id, true);
                if (!access) return;
                this.callDBFunc("deleteProject", access.project.id);
                await fs.promises.rm(
                    path.join(cfs.xPaintProjectDir, String(access.project.id)),
                    { recursive: true, force: true }
                );
                return res.json({ status: "ok" });
            } catch (err) {
                console.error("xPaint project deletion failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });

        this.route("get", "/api/mystuff/projects", async (req, res) => {
            const ownerid = req.session?.userId;
            if (!ownerid) return res.status(401).send("Not logged in.");
            try {
                await this.initDB();
                const stmt = this.db.getDb().prepare(
                    "SELECT id, title FROM projects WHERE ownerid = ?"
                );
                stmt.bind([ownerid]);

                const projects = [];
                while (stmt.step()) {
                    const project = stmt.getAsObject();
                    project.id = String(project.id);
                    projects.push(project);
                }
                stmt.free();
                return res.json(projects);
            } catch (err) {
                console.error("xPaint user project listing failed:", err);
                return res.status(500).json({ status: "Failed", error: err.message });
            }
        });
    }
}

module.exports = xPaint;
