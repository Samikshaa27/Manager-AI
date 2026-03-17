import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import Sidebar from '../components/Sidebar';
import { Briefcase, Clock, ChevronRight } from 'lucide-react';
import { projectService } from '../services/api';
import { formatDate } from '../lib/utils';
import { Link } from 'react-router-dom';

const ProjectsPage = ({ user, onLogout }) => {
  const [projects, setProjects] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    projectService.getAll().then(res => {
      setProjects(res.data);
      setLoading(false);
    });
  }, []);

  return (
    <div className="min-h-screen bg-transparent text-text flex relative">
      <Sidebar user={user} onLogout={onLogout} />
      <main className="flex-grow ml-72 p-12 relative z-10 w-full">
        <motion.header 
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          className="mb-12"
        >
          <h1 className="text-4xl font-heading font-extrabold mb-1 bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Projects</h1>
          <p className="text-text-muted font-medium">Manage and monitor all your AI-orchestrated projects.</p>
        </motion.header>

        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="glass-card p-0 overflow-hidden"
        >
          <div className="divide-y divide-white/10">
            {loading ? (
              <div className="p-20 flex flex-col items-center justify-center text-text-muted gap-4">
                <Clock className="animate-spin text-primary" size={32} />
                <span className="font-bold text-sm tracking-widest uppercase">Loading Projects...</span>
              </div>
            ) : projects.length === 0 ? (
              <div className="p-20 text-center text-text-muted italic font-medium">No projects found.</div>
            ) : projects.map((project, i) => (
              <motion.div
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.2 + i * 0.05 }}
                key={project.id}
              >
                <Link to={`/project/${project.id}`} className="block group hover:bg-white/5 transition-colors">
                  <div className="p-8 flex items-center gap-6">
                    <div className="w-14 h-14 rounded-2xl bg-white/5 border border-white/10 flex items-center justify-center text-primary group-hover:bg-primary group-hover:border-primary/50 group-hover:text-white transition-all duration-300 shadow-inner group-hover:shadow-[0_0_15px_rgba(139,92,246,0.5)]">
                      <Briefcase size={24} />
                    </div>
                    <div className="flex-grow">
                      <h3 className="text-xl font-bold group-hover:text-primary transition-colors">{project.name || project.projectName}</h3>
                      <div className="flex items-center gap-2 text-sm text-text-muted font-medium mt-1">
                        <Clock size={14} className="text-primary/70" />
                        Created {formatDate(project.createdAt)}
                      </div>
                    </div>
                    <div className="flex items-center gap-6">
                       <ChevronRight className="text-text-muted group-hover:text-primary group-hover:translate-x-1 transition-all" size={24} />
                    </div>
                  </div>
                </Link>
              </motion.div>
            ))}
          </div>
        </motion.div>
      </main>
    </div>
  );
};

export default ProjectsPage;
