import React, { Component } from 'react';
import { GridLayout } from "@egjs/react-infinitegrid";
import Utils from "../Utils";
import './Posts.css';

const Item = ({ post }) => (
  <div>
    {(!post.Photos || post.Photos.length === 0) && 
      <span>No photo</span>
    }
    {(post.Photos && post.Photos.length !== 0) &&
      <div className="photo-post"><a href={ "/post/" + post.Blogname + "/" + post.Id}> 
        <img src={Utils.GetSmallPhotoUrl(post)} width="250" data-id={post.Id} onLoad={this.imageReady} onError={this.imageReady} alt=""/>
      </a></div>
    }
  </div>
);

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { posts: [], loading: true, hasMore: true };

    this.onAppend = this.onAppend.bind(this);
    this.onLayoutComplete = this.onLayoutComplete.bind(this);
    this.imageReady = this.imageReady.bind(this);

    this.layoutInProgress = false;

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
      .then(response => response.json())
      .then(data => {
        this.setState({ posts: data, loading: false, hasMore: data.length === 50 });
      });
  }
  
  onAppend = ({ groupKey, startLoading }) => {
    if (!this.state.hasMore) {
      return;
    }

    startLoading();

    const [lastPost] = this.state.posts.slice(-1);
    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + this.props.match.params.blogname + "?after=" + lastPost.Id)
      .then(response => response.json())
      .then(data => {
        data.forEach(post => {
          post.GroupKey = groupKey;
        });
        this.setState(state => ({
          posts: state.posts.concat(data),
          hasMore: data.length === 50 
        }));
      });
  };

  onLayoutComplete = ({ isLayout, endLoading }) => {
    !isLayout && endLoading();

    if (!this.layoutInProgress) {
      this.layoutInProgress = true;
      this.masonryGrid.layout();
      this.layoutInProgress = false;
    }
  };

  imageReady() {
    this.masonryGrid.forcePack();
  }

  renderPostsTable(posts) {
    return (
      <GridLayout
        margin={10}
        align="center"
        onAppend={this.onAppend}
        onLayoutComplete={this.onLayoutComplete}
        transitionDuration={0.2}
        isConstantSize={true}
        ref={(child) => { this.masonryGrid = child; }}
      >
        {posts.map(post =>
          <Item groupKey={post.GroupKey} key={post.Id} post={post}/>
        )}
      </GridLayout>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.renderPostsTable(this.state.posts);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
      </div>
    );
  }
}
