import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vs, vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { Message, Role } from '../types';
import { Icons } from './icons';
import clsx from 'clsx';
import { motion, AnimatePresence } from 'framer-motion';
import { useStore } from '../store';
import { translations } from '../locales';
import { OpenClaw } from '@lobehub/icons';

interface MessageBubbleProps {
  message: Message;
  isLast: boolean;
}

const CodeBlock = ({ language, children }: { language: string, children?: React.ReactNode }) => {
  const [copied, setCopied] = useState(false);
  const lang = useStore(s => s.language);
  const settings = useStore(s => s.settings);
  const t = translations[lang];
  const isDark = settings.theme === 'dark';

  const handleCopy = () => {
    navigator.clipboard.writeText(String(children));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="relative group my-4 rounded-xl overflow-hidden border border-gray-200 dark:border-[#444746] shadow-sm bg-gray-50 dark:bg-[#1E1F20]">
      <div className="flex items-center justify-between px-4 py-2 bg-gray-100 dark:bg-[#2D2E2F] border-b border-gray-200 dark:border-[#444746]">
        <span className="text-xs font-mono text-gray-600 dark:text-[#C4C7C5] font-bold">{language || 'code'}</span>
        <button 
          onClick={handleCopy}
          className="flex items-center space-x-1.5 text-xs text-gray-500 dark:text-[#C4C7C5] hover:text-black dark:hover:text-white transition-colors opacity-0 group-hover:opacity-100 bg-gray-200 dark:bg-[#444746] px-2 py-1 rounded-md"
        >
          {copied ? <Icons.Check className="w-3 h-3 text-green-500" /> : <Icons.Copy className="w-3 h-3" />}
          <span>{copied ? t.copied : t.copy}</span>
        </button>
      </div>
      <SyntaxHighlighter
        style={isDark ? vscDarkPlus : vs}
        language={language}
        PreTag="div"
        customStyle={{
          margin: 0,
          borderRadius: 0,
          background: 'transparent',
          fontSize: '0.875rem',
          padding: '1.25rem',
          lineHeight: '1.6',
        }}
        codeTagProps={{
            className: "font-mono"
        }}
      >
        {String(children).replace(/\n$/, '')}
      </SyntaxHighlighter>
    </div>
  );
};

// Rich Media Components
const VideoWidget: React.FC<{ url: string }> = ({ url }) => (
    <div className="my-4 rounded-xl overflow-hidden shadow-lg border border-gray-200 dark:border-[#444746] bg-black">
        <video controls className="w-full aspect-video" src={url} poster="https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/images/BigBuckBunny.jpg">
            Your browser does not support the video tag.
        </video>
    </div>
);

const IframeWidget: React.FC<{ url: string, title?: string }> = ({ url, title }) => (
    <div className="my-4 rounded-xl overflow-hidden shadow-lg border border-gray-200 dark:border-[#444746] bg-white dark:bg-[#1E1F20]">
        <div className="bg-gray-100 dark:bg-[#2D2E2F] px-4 py-2 border-b border-gray-200 dark:border-[#444746] flex items-center gap-2">
            <div className="flex gap-1.5">
                <div className="w-2.5 h-2.5 rounded-full bg-red-400" />
                <div className="w-2.5 h-2.5 rounded-full bg-yellow-400" />
                <div className="w-2.5 h-2.5 rounded-full bg-green-400" />
            </div>
            <div className="flex-1 text-center text-xs text-gray-500 dark:text-[#C4C7C5] font-mono bg-white dark:bg-[#1E1F20] mx-4 rounded py-0.5 px-2 truncate">
                {url}
            </div>
        </div>
        <div className="relative w-full aspect-[4/3] bg-gray-50 dark:bg-[#131314] flex items-center justify-center">
             {/* Using a simulated iframe placeholder to avoid X-Frame-Options issues in demo */}
             <div className="text-center p-8">
                <Icons.Remote className="w-16 h-16 text-gray-300 dark:text-[#444746] mx-auto mb-4" />
                <h4 className="text-gray-900 dark:text-[#E3E3E3] font-bold mb-2">External Content Preview</h4>
                <p className="text-gray-500 dark:text-[#C4C7C5] text-sm mb-4">Content from {new URL(url).hostname}</p>
                <a href={url} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-2 px-4 py-2 bg-black dark:bg-[#A8C7FA] text-white dark:text-black rounded-lg text-sm font-bold hover:bg-gray-800 dark:hover:bg-[#8AB4F8] transition-colors">
                    <Icons.ArrowRight className="w-4 h-4" />
                    Open in New Tab
                </a>
             </div>
        </div>
    </div>
);

const OptionsWidget: React.FC<{ options: string[] }> = ({ options }) => (
    <div className="flex flex-wrap gap-2 my-4">
        {options.map((opt, idx) => (
            <button 
                key={idx}
                className="px-4 py-2 bg-white dark:bg-[#1E1F20] border border-gray-200 dark:border-[#444746] hover:border-black dark:hover:border-[#A8C7FA] hover:bg-gray-50 dark:hover:bg-[#333537] rounded-xl text-sm font-bold text-gray-700 dark:text-[#E3E3E3] transition-all shadow-sm active:scale-95"
                onClick={() => alert(`Selected option: ${opt}`)}
            >
                {opt}
            </button>
        ))}
    </div>
);

// Content Parser
const RichContentRenderer = ({ content }: { content: string }) => {
    // Regex for different widgets: [[TYPE:DATA]]
    // Splitting by lines or specific tags to interleave components
    const parts = content.split(/(\[\[(?:VIDEO|IFRAME|OPTIONS):.*?\]\])/g);

    return (
        <>
            {parts.map((part, index) => {
                const videoMatch = part.match(/^\[\[VIDEO:(.*?)\]\]$/);
                const iframeMatch = part.match(/^\[\[IFRAME:(.*?)\]\]$/);
                const optionsMatch = part.match(/^\[\[OPTIONS:(.*?)\]\]$/);

                if (videoMatch) {
                    return <VideoWidget key={index} url={videoMatch[1]} />;
                }
                if (iframeMatch) {
                    return <IframeWidget key={index} url={iframeMatch[1]} />;
                }
                if (optionsMatch) {
                    const opts = optionsMatch[1].split(',').map(s => s.trim());
                    return <OptionsWidget key={index} options={opts} />;
                }

                // Render standard markdown for non-widget parts
                if (!part.trim()) return null;

                return (
                    <React.Fragment key={index}>
                        <ReactMarkdown
                            remarkPlugins={[remarkGfm]}
                            components={{
                                code({ node, inline, className, children, ...props }: any) {
                                    const match = /language-(\w+)/.exec(className || '');
                                    return !inline && match ? (
                                        <CodeBlock language={match[1]}>{children}</CodeBlock>
                                    ) : (
                                        <code className={className} {...props}>
                                            {children}
                                        </code>
                                    );
                                }
                            }}
                        >
                            {part}
                        </ReactMarkdown>
                    </React.Fragment>
                );
            })}
        </>
    );
};

export const MessageBubble: React.FC<MessageBubbleProps> = ({ message, isLast }) => {
  const isUser = message.role === Role.USER;
  const userAvatar = useStore(s => s.user?.avatar);
  const [isHovered, setIsHovered] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleCopyMessage = () => {
    navigator.clipboard.writeText(message.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  
  return (
    <motion.div 
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className={clsx(
        "flex w-full mb-6 relative group",
        isUser ? "justify-end" : "justify-start"
      )}
    >
      <div className={clsx(
        "flex max-w-[90%] md:max-w-[80%]",
        isUser ? "flex-row-reverse" : "flex-row"
      )}>
        {/* Avatar */}
        <div className={clsx(
          "flex-shrink-0 w-9 h-9 rounded-full flex items-center justify-center mt-1 shadow-sm overflow-hidden",
          isUser ? "bg-gray-200 dark:bg-[#333537] ml-3" : "bg-black dark:bg-white dark:text-black mr-3"
        )}>
          {isUser ? (
             userAvatar ? (
               <img src={userAvatar} className="w-full h-full object-cover" />
             ) : (
               <div className="w-full h-full rounded-full bg-gradient-to-br from-gray-100 to-gray-300 dark:from-[#333537] dark:to-[#444746]" />
             )
          ) : (
             <div className="w-full h-full flex items-center justify-center">
                <OpenClaw.Color size="100%" className="w-full h-full" />
             </div>
          )}
        </div>

        {/* Content */}
        <div className={clsx(
          "flex flex-col",
          isUser ? "items-end" : "items-start",
          "min-w-0 flex-1"
        )}>
          {/* Attachments if any */}
          {message.attachments && message.attachments.length > 0 && (
             <div className={clsx("flex flex-wrap gap-2 mb-2", isUser ? "justify-end" : "justify-start")}>
                 {message.attachments.map((att, idx) => (
                     <div key={idx} className="flex items-center gap-2 bg-white dark:bg-[#1E1F20] px-3 py-2 rounded-lg border border-gray-200 dark:border-[#444746] shadow-sm text-sm">
                        <Icons.Attach className="w-4 h-4 text-gray-500 dark:text-[#C4C7C5]" />
                        <span className="text-gray-700 dark:text-[#E3E3E3] max-w-[200px] truncate">{att.name}</span>
                     </div>
                 ))}
             </div>
          )}

          <div className={clsx(
            "relative text-[17px] leading-relaxed max-w-full overflow-hidden",
            isUser 
              ? "px-5 py-3.5 bg-white dark:bg-[#333537] border border-gray-200 dark:border-transparent text-gray-900 dark:text-[#E3E3E3] rounded-2xl rounded-tr-sm shadow-sm" 
              : "py-1 px-0 bg-transparent text-gray-900 dark:text-[#E3E3E3] w-full" 
          )}>
             {isUser ? (
               <div className="whitespace-pre-wrap break-words">{message.content}</div>
             ) : (
               <div className="prose prose-base max-w-none prose-headings:font-semibold prose-headings:text-gray-900 dark:prose-headings:text-[#E3E3E3] prose-p:text-gray-800 dark:prose-p:text-[#C4C7C5] prose-a:text-blue-600 dark:prose-a:text-[#A8C7FA] prose-strong:text-gray-900 dark:prose-strong:text-white prose-code:text-red-500 dark:prose-code:text-[#F28B82] prose-code:bg-gray-100 dark:prose-code:bg-[#1E1F20] prose-code:px-1 prose-code:py-0.5 prose-code:rounded-md prose-pre:p-0 prose-pre:bg-transparent break-words">
                  <RichContentRenderer content={message.content || ''} />
                  {message.isStreaming && (
                      <span className="inline-block w-1.5 h-5 ml-1 align-middle bg-black dark:bg-white animate-pulse" />
                  )}
               </div>
             )}
          </div>
          
          {/* Timestamp & Actions */}
          <div className={clsx(
             "flex items-center gap-3 mt-1 px-1",
             isUser ? "flex-row-reverse" : "flex-row"
          )}>
             <span className="text-[11px] text-gray-400 dark:text-[#C4C7C5] font-medium">
                {new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
             </span>

             <AnimatePresence>
                {isHovered && !message.isStreaming && (
                    <motion.div 
                        initial={{ opacity: 0, y: 5 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: 5 }}
                        className="flex items-center gap-1.5"
                    >
                        <button 
                            onClick={handleCopyMessage}
                            title="Copy message"
                            className="p-1.5 text-gray-400 dark:text-[#C4C7C5] hover:text-black dark:hover:text-[#E3E3E3] hover:bg-gray-200 dark:hover:bg-[#333537] rounded-md transition-all"
                        >
                            {copied ? <Icons.Check className="w-3.5 h-3.5 text-green-600 dark:text-green-400" /> : <Icons.Copy className="w-3.5 h-3.5" />}
                        </button>
                        <button 
                            title="Share"
                            className="p-1.5 text-gray-400 dark:text-[#C4C7C5] hover:text-black dark:hover:text-[#E3E3E3] hover:bg-gray-200 dark:hover:bg-[#333537] rounded-md transition-all"
                        >
                            <Icons.Mail className="w-3.5 h-3.5" />
                        </button>
                    </motion.div>
                )}
             </AnimatePresence>
          </div>
        </div>
      </div>
    </motion.div>
  );
};
